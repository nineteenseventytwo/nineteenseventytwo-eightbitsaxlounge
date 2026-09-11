"""Application configuration settings loaded from environment variables."""

from pydantic import ConfigDict
from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Application settings loaded from environment variables."""
    
    model_config = ConfigDict(
        env_file=".env",
        case_sensitive=False
    )
    
    # Twitch Configuration
    # str, not int: twitchio passes these straight through into the
    # EventSub subscription condition without coercing, and Twitch rejects
    # a numeric user_id with "missing or unparseable subscription
    # condition". They are also compared against the TEXT user_id column in
    # tokens.db, which an int never equals.
    twitch_bot_id: str  # Twitch User ID for EightBitSaxBot (can be found via Twitch API)
    twitch_owner_id: str  # Twitch User ID for the bot owner
    twitch_client_id: str  # Your app's Client ID from dev.twitch.tv
    twitch_client_secret: str  # Your app's Client Secret from dev.twitch.tv
    twitch_channel: str  # Channel name to connect to
    twitch_prefix: str = "!"
    
    # MIDI API Configuration
    midi_device_url: str = "http://eightbitsaxlounge-midi-service:8080"
    midi_api_timeout: int = 30
    
    # MIDI Authentication
    midi_client_id: str
    midi_client_secret: str
    
    # MIDI Device Configuration
    midi_device_name: str = "One Series Ventris Reverb"
    
    # Valid engine names for VentrisDualReverb
    valid_engines: list[str] = [
        "Room",
        "Hall",
        "EDome",
        "TrueSpring",
        "Plate",
        "LoFi",
        "ModVerb",
        "Shimmer",
        "EchoVerb",
        "Swell",
        "Offspring",
        "Reverse",
        "OutboardSpring",
        "MetalBox"
    ]
    
    # Bot Configuration
    bot_name: str = "EightBitSaxBot"
    log_level: str = "INFO"

    # NATS Configuration
    nats_url: str = "nats://eightbitsaxlounge-state-client:4222"
    nats_user: str = "chat"
    nats_pass: str = ""


# Global settings instance
settings = Settings()
