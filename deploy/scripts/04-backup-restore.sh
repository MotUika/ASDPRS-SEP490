#!/bin/bash
# Script 4: Backup and Restore Database
# Usage: ./04-backup-restore.sh [backup|restore|list]

set -e

BACKUP_DIR="/opt/asdprs/backups"
DB_CONTAINER="asdprs-sqlserver"
DB_NAME="LMS_ASDPRS"
DB_PASSWORD="YourStrong@Passw0rd123"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

show_help() {
    echo "ASDPRS Database Backup/Restore"
    echo ""
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  backup  - Create database backup"
    echo "  restore - Restore from backup (interactive)"
    echo "  list    - List available backups"
    echo ""
}

backup_db() {
    echo "Creating backup..."
    BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_${TIMESTAMP}.bak"
    
    docker exec $DB_CONTAINER /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "$DB_PASSWORD" -C \
        -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'/var/opt/mssql/backup/${DB_NAME}_${TIMESTAMP}.bak' WITH FORMAT, INIT, NAME = '${DB_NAME}-Full Database Backup'"
    
    # Copy backup from container
    docker cp $DB_CONTAINER:/var/opt/mssql/backup/${DB_NAME}_${TIMESTAMP}.bak $BACKUP_FILE
    
    echo "Backup created: $BACKUP_FILE"
    echo "Size: $(du -h $BACKUP_FILE | cut -f1)"
}

restore_db() {
    echo "Available backups:"
    ls -la $BACKUP_DIR/*.bak 2>/dev/null || echo "No backups found"
    echo ""
    read -p "Enter backup filename to restore: " BACKUP_NAME
    
    if [ ! -f "$BACKUP_DIR/$BACKUP_NAME" ]; then
        echo "Error: Backup file not found"
        exit 1
    fi
    
    echo "WARNING: This will overwrite the current database!"
    read -p "Are you sure? (y/N): " confirm
    
    if [ "$confirm" = "y" ] || [ "$confirm" = "Y" ]; then
        # Copy backup to container
        docker cp "$BACKUP_DIR/$BACKUP_NAME" $DB_CONTAINER:/var/opt/mssql/backup/
        
        # Restore database
        docker exec $DB_CONTAINER /opt/mssql-tools18/bin/sqlcmd \
            -S localhost -U sa -P "$DB_PASSWORD" -C \
            -Q "RESTORE DATABASE [$DB_NAME] FROM DISK = N'/var/opt/mssql/backup/$BACKUP_NAME' WITH REPLACE"
        
        echo "Database restored successfully!"
    else
        echo "Cancelled."
    fi
}

list_backups() {
    echo "Available backups in $BACKUP_DIR:"
    echo ""
    ls -lh $BACKUP_DIR/*.bak 2>/dev/null || echo "No backups found"
}

case "$1" in
    backup)
        backup_db
        ;;
    restore)
        restore_db
        ;;
    list)
        list_backups
        ;;
    *)
        show_help
        ;;
esac
