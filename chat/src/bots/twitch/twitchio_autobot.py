import asqlite
import logging
import twitchio
from twitchio import eventsub
from twitchio.ext import commands

from bots.twitch.eightbitsaxlounge_component import EightBitSaxLoungeComponent
from config.settings import settings


LOGGER: logging.Logger = logging.getLogger("TwitchioAutoBot")
CLIENT_ID: str = settings.twitch_client_id  # The CLIENT ID from the Twitch Dev Console
CLIENT_SECRET: str = settings.twitch_client_secret  # The CLIENT SECRET from the Twitch Dev Console
BOT_ID: str = settings.twitch_bot_id  # The Account ID of the bot user...
OWNER_ID: str = settings.twitch_owner_id  # Your personal User ID..


class TwitchioAutoBot(commands.AutoBot):
    """TwitchIO AutoBot with token management and event subscription."""
    def __init__(self, *, token_database: asqlite.Pool, subs: list[eventsub.SubscriptionPayload]) -> None:
        self.token_database = token_database

        super().__init__(
            client_id=CLIENT_ID,
            client_secret=CLIENT_SECRET,
            bot_id=BOT_ID,
            owner_id=OWNER_ID,
            prefix="!",
            subscriptions=subs,
            force_subscribe=True,
        )

    async def setup_hook(self) -> None:
        """Called after the bot is ready. Add custom components that e.g. define commands."""
        # Add 8bsl component which contains our commands...
        await self.add_component(EightBitSaxLoungeComponent())

    async def event_oauth_authorized(self, payload: twitchio.authentication.UserTokenPayload) -> None:
        """Called when a user authorizes the bot and provides tokens. Store tokens and subscribe to events for the authorized user."""
        await self._add_token(payload.access_token, payload.refresh_token)

        if not payload.user_id:
            return

        if payload.user_id == self.bot_id:
            # We usually don't want to subscribe to events on the bots channel...
            return

        # A list of subscriptions we would like to make to the newly authorized channel...
        subs: list[eventsub.SubscriptionPayload] = [
            eventsub.ChatMessageSubscription(broadcaster_user_id=payload.user_id, user_id=self.bot_id),
        ]

        resp: twitchio.MultiSubscribePayload = await self.multi_subscribe(subs)
        if resp.errors:
            LOGGER.warning("Failed to subscribe to: %r, for user: %s", resp.errors, payload.user_id)

    async def event_ready(self) -> None:
        """Called when the bot is ready."""
        LOGGER.info("Successfully logged in as: %s", self.bot_id)
    
    async def _add_token(self, token: str, refresh: str) -> twitchio.authentication.ValidateTokenPayload:
        """Add a token to the bot and store it in the database."""
        # Make sure to call super() as it will add the tokens interally and return us some data...
        resp: twitchio.authentication.ValidateTokenPayload = await super().add_token(token, refresh)

        # Store our tokens in a simple SQLite Database when they are authorized...
        query = """
        INSERT INTO tokens (user_id, token, refresh)
        VALUES (?, ?, ?)
        ON CONFLICT(user_id)
        DO UPDATE SET
            token = excluded.token,
            refresh = excluded.refresh;
        """

        async with self.token_database.acquire() as connection:
            await connection.execute(query, (resp.user_id, token, refresh))

        LOGGER.info("Added token to the database for user: %s", resp.user_id)
        return resp
