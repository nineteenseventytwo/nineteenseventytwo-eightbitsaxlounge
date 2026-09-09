// Service implementing CRUD operations against CouchDB over HTTP.
//
// General structure:
//   - Each method constructs the CouchDB REST URL (using couchURL + path).
//   - It sends the appropriate HTTP method (GET/POST/PUT/DELETE).
//   - It checks the expected status code and decodes/returns JSON when needed.
//   - Errors include the HTTP status and response body to aid diagnostics.
//
// Configuration/testing:
//   - httpClient is a package-level var so tests can replace it with a mock.
//   - couchURL is built from environment variables: COUCHDB_USER,
//     COUCHDB_PASSWORD, COUCHDB_ENDPOINT.
package gofiles

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"os"
)

var httpClient = http.DefaultClient

type ProdCouchService struct{}

// GetDocumentByDatabaseNameAndDocumentId performs: GET /{dbname}/{id}
// Returns the JSON-decoded document on 200 OK; otherwise an error containing
// status and body. The map includes CouchDB metadata fields like _id and _rev.
func (s *ProdCouchService) GetDocumentByDatabaseNameAndDocumentId(dbname string, id string) (map[string]interface{}, error) {
	docURL := fmt.Sprintf("%s/%s/%s", couchURL(), dbname, id)
	resp, err := httpClient.Get(docURL)
	if err != nil {
		return nil, fmt.Errorf("failed to send GET request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("GET request failed with status %d: %s", resp.StatusCode, string(body))
	}

	var result map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, fmt.Errorf("failed to decode response body: %w", err)
	}
	return result, nil
}

// CreateDocumentByDatabaseName performs: POST /{dbname}
// Expects a JSON document in the request body; responds with 201 Created on
// success. Returns error with status/body if creation fails.
func (s *ProdCouchService) CreateDocumentByDatabaseName(dbname string, doc map[string]interface{}) error {
	b, err := json.Marshal(doc)
	if err != nil {
		return fmt.Errorf("failed to marshal document: %w", err)
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL(), dbname)

	req, err := http.NewRequest(http.MethodPost, dbURL, bytes.NewReader(b))
	if err != nil {
		return fmt.Errorf("failed to create POST request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send POST request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("POST request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// UpdateDocumentByDatabaseNameAndDocumentId performs: PUT /{dbname}/{id}
// Expects a JSON document in the request body; responds with 200 OK on
// success. The caller should include the correct _rev within the document if
// using CouchDB's MVCC (not enforced here).
func (s *ProdCouchService) UpdateDocumentByDatabaseNameAndDocumentId(dbname string, id string, doc map[string]interface{}) error {
	// If caller did not supply a _rev we need to fetch current revision first.
	rev, hasRev := doc["_rev"].(string)
	if !hasRev || rev == "" {
		existing, err := s.GetDocumentByDatabaseNameAndDocumentId(dbname, id)
		if err != nil {
			return fmt.Errorf("missing _rev and failed to retrieve existing document: %w", err)
		}
		existingRev, ok := existing["_rev"].(string)
		if !ok || existingRev == "" {
			return fmt.Errorf("existing document does not contain a valid _rev")
		}
		doc["_rev"] = existingRev
	}

	b, err := json.Marshal(doc)
	if err != nil {
		return fmt.Errorf("failed to marshal document: %w", err)
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL(), dbname)

	req, err := http.NewRequest(http.MethodPut, fmt.Sprintf("%s/%s", dbURL, id), bytes.NewReader(b))
	if err != nil {
		return fmt.Errorf("failed to create PUT request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send PUT request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	// Accept both 200 OK and 201 Created as success responses from CouchDB
	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusCreated {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("PUT request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// DeleteDocumentByDatabaseNameAndDocumentId performs: DELETE /{dbname}/{id}?rev={rev}
// It first fetches the doc to retrieve its current _rev, then issues the
// DELETE with that revision. Returns 200 OK on success.
func (s *ProdCouchService) DeleteDocumentByDatabaseNameAndDocumentId(dbname string, id string) error {
	doc, err := s.GetDocumentByDatabaseNameAndDocumentId(dbname, id)
	if err != nil {
		return fmt.Errorf("failed to retrieve document for deletion: %w", err)
	}

	rev, ok := doc["_rev"].(string)
	if !ok {
		return fmt.Errorf("document does not contain a valid _rev field")
	}

	dbURL := fmt.Sprintf("%s/%s", couchURL(), dbname)

	url := fmt.Sprintf("%s/%s?rev=%s", dbURL, id, rev)

	req, err := http.NewRequest(http.MethodDelete, url, nil)
	if err != nil {
		return fmt.Errorf("failed to create DELETE request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send DELETE request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("DELETE request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// GetDatabaseByName performs: GET /{dbname}
// Returns database info (e.g., doc count) on 200 OK; otherwise error.
func (s *ProdCouchService) GetDatabaseByName(dbName string) (map[string]interface{}, error) {
	req, err := http.NewRequest(http.MethodGet, fmt.Sprintf("%s/%s", couchURL(), dbName), nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create GET request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("failed to send GET request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("GET request failed with status %d: %s", resp.StatusCode, string(body))
	}

	var result map[string]interface{}
	if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
		return nil, fmt.Errorf("failed to decode response body: %w", err)
	}
	return result, nil
}

// CreateDatabaseByName performs: PUT /{dbname}
// Treats 201 Created as success, and 412 Precondition Failed (already exists)
// as a no-op success to keep the operation idempotent.
func (s *ProdCouchService) CreateDatabaseByName(dbName string) error {
	req, err := http.NewRequest(http.MethodPut, fmt.Sprintf("%s/%s", couchURL(), dbName), nil)
	if err != nil {
		return fmt.Errorf("failed to create PUT request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send PUT request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusCreated && resp.StatusCode != http.StatusPreconditionFailed {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("failed to create database, status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// DeleteDatabaseByName performs: DELETE /{dbname}
// Treats 200 OK as success. Returns error for other status codes.
func (s *ProdCouchService) DeleteDatabaseByName(dbName string) error {
	req, err := http.NewRequest(http.MethodDelete, fmt.Sprintf("%s/%s", couchURL(), dbName), nil)
	if err != nil {
		return fmt.Errorf("failed to create DELETE request: %w", err)
	}

	resp, err := httpClient.Do(req)
	if err != nil {
		return fmt.Errorf("failed to send DELETE request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("DELETE request failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// GetDocumentsByDatabaseName fetches all documents from the database using
// GET /{db}/_all_docs?include_docs=true and returns the embedded docs.
func (s *ProdCouchService) GetDocumentsByDatabaseName(dbName string) ([]map[string]interface{}, error) {
	url := fmt.Sprintf("%s/%s/_all_docs?include_docs=true", couchURL(), dbName)
	req, err := http.NewRequest(http.MethodGet, url, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create GET request: %w", err)
	}
	resp, err := httpClient.Do(req)
	if err != nil {
		return nil, fmt.Errorf("failed to send GET request: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()
	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return nil, fmt.Errorf("_all_docs failed with status %d: %s", resp.StatusCode, string(body))
	}
	var parsed struct {
		Rows []struct {
			Doc map[string]interface{} `json:"doc"`
		} `json:"rows"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&parsed); err != nil {
		return nil, fmt.Errorf("failed to decode _all_docs response: %w", err)
	}
	docs := make([]map[string]interface{}, 0, len(parsed.Rows))
	for _, row := range parsed.Rows {
		if row.Doc != nil {
			docs = append(docs, row.Doc)
		}
	}
	return docs, nil
}

// CheckHealth performs: GET /_up
// Checks if CouchDB is accessible and responding.
// Returns nil if healthy (200 OK), error otherwise.
func (s *ProdCouchService) CheckHealth() error {
	upURL := fmt.Sprintf("%s/_up", couchURL())
	resp, err := httpClient.Get(upURL)
	if err != nil {
		return fmt.Errorf("failed to connect to CouchDB: %w", err)
	}
	defer func() { _ = resp.Body.Close() }()

	if resp.StatusCode != http.StatusOK {
		body, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("CouchDB health check failed with status %d: %s", resp.StatusCode, string(body))
	}

	return nil
}

// couchURL builds the base CouchDB URL with embedded basic-auth credentials.
//
//	http://COUCHDB_USER:COUCHDB_PASSWORD@COUCHDB_ENDPOINT
//
// Built with net/url rather than fmt.Sprintf: a raw Sprintf embeds the
// password into the URL's userinfo component with no escaping, so any
// generated password containing '/', '@', ':', or other URL-significant
// characters corrupts the URL outright — confirmed live 2026-09-09
// against a Vault-generated password containing '/', which broke every
// call through couchURL() including the /health check, with no error
// surfaced beyond a generic connection/status failure. url.UserPassword
// percent-encodes the userinfo component per RFC 3986; every call site
// still receives a plain string via .String(), so nothing downstream
// changes.
func couchURL() string {
	u := url.URL{
		Scheme: "http",
		User:   url.UserPassword(os.Getenv("COUCHDB_USER"), os.Getenv("COUCHDB_PASSWORD")),
		Host:   os.Getenv("COUCHDB_ENDPOINT"),
	}
	return u.String()
}
