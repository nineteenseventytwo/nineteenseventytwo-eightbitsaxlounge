"""Tests for HelpHandler."""

import pytest
from unittest.mock import AsyncMock
from commands.handlers.help import HelpHandler, HELP_TOPICS


@pytest.fixture
def mock_publisher():
    publisher = AsyncMock()
    publisher.publish = AsyncMock()
    return publisher


@pytest.fixture
def help_handler(mock_publisher):
    return HelpHandler(nats_publisher=mock_publisher)


@pytest.fixture
def help_handler_no_nats():
    return HelpHandler(nats_publisher=None)


class TestHelpHandler:
    """Test cases for HelpHandler."""

    @pytest.mark.asyncio
    async def test_no_args_publishes_overlay_help(self, help_handler, mock_publisher, mock_twitch_context):
        """!help with no args publishes an empty event to overlay.help."""
        response = await help_handler.handle([], mock_twitch_context)
        mock_publisher.publish.assert_awaited_once_with('overlay.help', '')
        assert isinstance(response, str)
        assert '🎵' in response

    @pytest.mark.asyncio
    async def test_valid_topic_publishes_overlay_popup(self, help_handler, mock_publisher, mock_twitch_context):
        """!help engine publishes overlay.popup with value 'engine'."""
        response = await help_handler.handle(['engine'], mock_twitch_context)
        mock_publisher.publish.assert_awaited_once_with('overlay.popup', 'engine')
        assert '🎵' in response

    @pytest.mark.asyncio
    async def test_valid_topic_lofi(self, help_handler, mock_publisher, mock_twitch_context):
        """!help lofi publishes overlay.popup with value 'lofi'."""
        await help_handler.handle(['lofi'], mock_twitch_context)
        mock_publisher.publish.assert_awaited_once_with('overlay.popup', 'lofi')

    @pytest.mark.asyncio
    async def test_topic_matching_is_case_insensitive(self, help_handler, mock_publisher, mock_twitch_context):
        """Topic lookup is case-insensitive."""
        await help_handler.handle(['ENGINE'], mock_twitch_context)
        mock_publisher.publish.assert_awaited_once_with('overlay.popup', 'engine')

    @pytest.mark.asyncio
    async def test_unknown_topic_returns_error(self, help_handler, mock_publisher, mock_twitch_context):
        """!help unknown returns an error and does not publish anything."""
        response = await help_handler.handle(['unknown'], mock_twitch_context)
        mock_publisher.publish.assert_not_awaited()
        assert '❌' in response
        assert 'unknown' in response

    @pytest.mark.asyncio
    async def test_unknown_topic_lists_valid_topics(self, help_handler, mock_publisher, mock_twitch_context):
        """Error response includes the list of valid topics."""
        response = await help_handler.handle(['bad'], mock_twitch_context)
        for topic in HELP_TOPICS:
            assert topic in response

    @pytest.mark.asyncio
    async def test_no_nats_does_not_raise(self, help_handler_no_nats, mock_twitch_context):
        """Handler with no publisher does not raise when publishing would occur."""
        response = await help_handler_no_nats.handle([], mock_twitch_context)
        assert isinstance(response, str)

    @pytest.mark.asyncio
    async def test_help_topics_contains_expected_values(self, help_handler, mock_twitch_context):
        """Sanity-check that HELP_TOPICS includes the initial set."""
        assert 'engine' in HELP_TOPICS
        assert 'lofi' in HELP_TOPICS
