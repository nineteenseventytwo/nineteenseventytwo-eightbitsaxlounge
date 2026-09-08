"""
Lightweight HTTP health check server for Kubernetes probes.
Runs alongside the Twitch bot to provide liveness and readiness endpoints.
"""

import logging
from aiohttp import web

logger = logging.getLogger(__name__)


class HealthServer:
    """Simple HTTP server for health checks."""
    
    def __init__(self, port: int = 8080, bot_instance=None):
        """
        Initialize health server.
        
        Args:
            port: Port to listen on (default 8080)
            bot_instance: Reference to bot for health status checks
        """
        self.port = port
        self.bot = bot_instance
        self.app = web.Application()
        self.runner = None
        self._setup_routes()
    
    def _setup_routes(self):
        """Configure health check routes."""
        self.app.router.add_get('/health', self.health_check)
        self.app.router.add_get('/ready', self.readiness_check)
    
    async def health_check(self, request):
        """
        Liveness probe endpoint.
        Returns 200 if the server is running.
        """
        return web.json_response({
            'status': 'healthy',
            'service': 'eightbitsaxlounge-chat'
        })
    
    #TODO: review
    async def readiness_check(self, request):
        """
        Readiness probe endpoint.
        Returns 200 if bot is connected and ready, 503 otherwise.
        """
        if self.bot and hasattr(self.bot, 'is_ready') and not self.bot.is_ready():
            return web.json_response(
                {'status': 'not_ready', 'message': 'bot not connected'},
                status=503
            )
        
        return web.json_response({
            'status': 'ready',
            'service': 'eightbitsaxlounge-chat'
        })
    
    async def start(self):
        """Start the health check server."""
        self.runner = web.AppRunner(self.app)
        await self.runner.setup()
        site = web.TCPSite(self.runner, '0.0.0.0', self.port)
        await site.start()
        logger.info(f'Health server started on port {self.port}')
    
    async def stop(self):
        """Stop the health check server."""
        if self.runner:
            await self.runner.cleanup()
            logger.info('Health server stopped')
