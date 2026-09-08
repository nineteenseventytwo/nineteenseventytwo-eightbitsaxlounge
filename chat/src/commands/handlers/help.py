"""Help command handler — triggers overlay help screens or topic popups via NATS."""

import logging
from typing import Any

from commands.handlers.command_handler import CommandHandler

logger = logging.getLogger(__name__)

# Topics that have a corresponding popup image in /images/popups/<topic>.png
HELP_TOPICS: list[str] = ['engine', 'lofi']


class HelpHandler(CommandHandler):
    """Handler for !help commands.

    - !help          → publishes an empty event to overlay.help (triggers the full help
                       image cycle on the overlay).
    - !help <topic>  → publishes the topic value to overlay.popup so the matching popup
                       image is shown immediately (e.g. !help engine shows engine.png).
                       Responds with an error if the topic is not in HELP_TOPICS.
    """

    def __init__(self, nats_publisher=None):
        """Initialise the handler.

        Args:
            nats_publisher: NatsPublisher instance used to emit overlay events.
                            May be None in unit-test contexts where publishing is mocked.
        """
        super().__init__()
        self._nats = nats_publisher

    @property
    def command_name(self) -> str:
        return "help"

    @property
    def description(self) -> str:
        topics = ', '.join(HELP_TOPICS)
        return f"Show overlay help screens. Usage: !help or !help <topic>. Topics: {topics}"

    async def handle(self, args: list[str], context: Any) -> str:
        """
        Handle !help commands.

        Args:
            args: Optional topic argument.
            context: Twitch command context.

        Returns:
            A single chat response string.
        """
        if not args:
            # No topic — trigger the full help image cycle on the overlay.
            if self._nats:
                await self._nats.publish('overlay.help', '')
            logger.info('Help cycle triggered via overlay.help')
            return '🎵 Showing help on the overlay!'

        topic = args[0].lower()
        if topic not in HELP_TOPICS:
            valid = ', '.join(HELP_TOPICS)
            return f"❌ No help topic '{topic}'. Available topics: {valid}"

        # Valid topic — show the matching popup image.
        if self._nats:
            await self._nats.publish('overlay.popup', topic)
        logger.info("Help popup triggered for topic '%s'", topic)
        return f'🎵 Showing help for {topic} on the overlay!'
