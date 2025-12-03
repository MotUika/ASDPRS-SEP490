#!/bin/bash
# Script 3: Manage ASDPRS Services
# Usage: ./03-manage-services.sh [start|stop|restart|status|logs|rebuild]

set -e

APP_DIR="/opt/asdprs"
COMPOSE_FILE="$APP_DIR/deploy/docker-compose.yml"

show_help() {
    echo "ASDPRS Service Manager"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  start     - Start all services"
    echo "  stop      - Stop all services"
    echo "  restart   - Restart all services"
    echo "  status    - Show service status"
    echo "  logs      - Show logs (follow mode)"
    echo "  logs-api  - Show API logs only"
    echo "  logs-db   - Show SQL Server logs only"
    echo "  rebuild   - Rebuild and restart API"
    echo "  clean     - Stop and remove all containers, volumes"
    echo "  shell-api - Open shell in API container"
    echo "  shell-db  - Open SQL shell"
    echo ""
}

case "$1" in
    start)
        echo "Starting services..."
        docker compose -f $COMPOSE_FILE up -d
        docker compose -f $COMPOSE_FILE ps
        ;;
    stop)
        echo "Stopping services..."
        docker compose -f $COMPOSE_FILE down
        ;;
    restart)
        echo "Restarting services..."
        docker compose -f $COMPOSE_FILE restart
        docker compose -f $COMPOSE_FILE ps
        ;;
    status)
        echo "Service Status:"
        docker compose -f $COMPOSE_FILE ps
        echo ""
        echo "Resource Usage:"
        docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}"
        ;;
    logs)
        docker compose -f $COMPOSE_FILE logs -f
        ;;
    logs-api)
        docker compose -f $COMPOSE_FILE logs -f api
        ;;
    logs-db)
        docker compose -f $COMPOSE_FILE logs -f sqlserver
        ;;
    rebuild)
        echo "Rebuilding API..."
        docker compose -f $COMPOSE_FILE up -d --build api
        docker compose -f $COMPOSE_FILE ps
        ;;
    clean)
        echo "WARNING: This will remove all containers and data!"
        read -p "Are you sure? (y/N): " confirm
        if [ "$confirm" = "y" ] || [ "$confirm" = "Y" ]; then
            docker compose -f $COMPOSE_FILE down -v --remove-orphans
            echo "Cleaned up!"
        else
            echo "Cancelled."
        fi
        ;;
    shell-api)
        docker exec -it asdprs-api /bin/bash
        ;;
    shell-db)
        docker exec -it asdprs-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "YourStrong@Passw0rd123" -C
        ;;
    *)
        show_help
        ;;
esac
