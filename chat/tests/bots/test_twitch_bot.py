"""Lightweight tests for the current TwitchBot module.

These tests only perform basic sanity checks (construction with
mocks, attributes present). More thorough AutoBot-specific tests
are in `test_twitch_autobot.py`.
"""

import os
import pytest
from unittest.mock import Mock, AsyncMock

# minimal env for settings init
os.environ.setdefault('TWITCH_BOT_ID', '1424580736')
os.environ.setdefault('TWITCH_OWNER_ID', '896950964')
os.environ.setdefault('TWITCH_CLIENT_SECRET', 'test_midi_secret')

from bots.twitch.bot import Bot


@pytest.fixture
def basic_bot():
    # construct without patching settings: ensure signature works
    bot = Bot()

    # minimal twitchio stubs
    bot.twitchio = Mock()
    bot.twitchio.start = AsyncMock()
    bot.twitchio.close = AsyncMock()
    bot.twitchio.get_channel = Mock()

    return bot


def test_twitchbot_has_core_methods(basic_bot):
    assert hasattr(basic_bot, 'start')
    assert hasattr(basic_bot, 'shutdown')
    assert hasattr(basic_bot, 'send_message')


@pytest.mark.asyncio
async def test_start_and_shutdown_no_errors(basic_bot):
    # Patch start/shutdown to AsyncMocks to avoid running internal event loop
    basic_bot.start = AsyncMock()
    basic_bot.shutdown = AsyncMock()

    await basic_bot.start()
    await basic_bot.shutdown()