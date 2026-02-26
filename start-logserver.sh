#!/bin/bash

# EasySave Log Server - Quick Start Script (Linux/Mac)

# Colors
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

echo -e "${CYAN}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║           EasySave Distributed Log Server v1.0           ║${NC}"
echo -e "${CYAN}║                   Docker Quick Start                      ║${NC}"
echo -e "${CYAN}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if Docker is running
echo -e "${YELLOW}Checking Docker status...${NC}"
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}✗ Docker is not running. Please start Docker.${NC}"
    echo ""
    read -p "Press Enter to exit"
    exit 1
fi
echo -e "${GREEN}✓ Docker is running${NC}"

echo ""
echo -e "${CYAN}Select an option:${NC}"
echo -e "${WHITE}1. Start server (docker-compose)${NC}"
echo -e "${WHITE}2. Stop server${NC}"
echo -e "${WHITE}3. View logs${NC}"
echo -e "${WHITE}4. Rebuild and restart server${NC}"
echo -e "${WHITE}5. View server status${NC}"
echo -e "${WHITE}6. Exit${NC}"
echo ""

read -p "Enter your choice (1-6): " choice

case $choice in
    1)
        echo ""
        echo -e "${YELLOW}Starting EasySave Log Server...${NC}"
        docker-compose up -d
        if [ $? -eq 0 ]; then
            echo ""
            echo -e "${GREEN}✓ Server started successfully!${NC}"
            echo ""
            echo -e "${CYAN}Server is listening on port 5555${NC}"
            echo -e "${CYAN}Logs are saved to: $(pwd)/logs${NC}"
            echo ""
            echo -e "${YELLOW}To view logs in real-time, run:${NC}"
            echo -e "${WHITE}  docker logs -f easysave-logserver${NC}"
            echo ""
            echo -e "${YELLOW}To configure EasySave clients:${NC}"
            echo -e "${WHITE}  1. Open EasySave application${NC}"
            echo -e "${WHITE}  2. Go to Settings (Paramètres) tab${NC}"
            echo -e "${WHITE}  3. Enable 'Remote Logging'${NC}"
            echo -e "${WHITE}  4. Enter server IP address${NC}"
            echo -e "${WHITE}  5. Port: 5555${NC}"
        else
            echo -e "${RED}✗ Failed to start server${NC}"
        fi
        ;;
    2)
        echo ""
        echo -e "${YELLOW}Stopping EasySave Log Server...${NC}"
        docker-compose down
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}✓ Server stopped${NC}"
        fi
        ;;
    3)
        echo ""
        echo -e "${YELLOW}Showing real-time logs (Press Ctrl+C to stop)...${NC}"
        echo ""
        docker logs -f easysave-logserver
        ;;
    4)
        echo ""
        echo -e "${YELLOW}Rebuilding and restarting server...${NC}"
        docker-compose down
        docker-compose build --no-cache
        docker-compose up -d
        if [ $? -eq 0 ]; then
            echo ""
            echo -e "${GREEN}✓ Server rebuilt and restarted successfully!${NC}"
        fi
        ;;
    5)
        echo ""
        echo -e "${CYAN}Server Status:${NC}"
        echo ""
        docker ps -a --filter "name=easysave-logserver" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
        echo ""
        echo -e "${CYAN}Latest logs:${NC}"
        docker logs --tail 20 easysave-logserver
        ;;
    6)
        echo ""
        echo -e "${CYAN}Goodbye!${NC}"
        exit 0
        ;;
    *)
        echo ""
        echo -e "${RED}Invalid choice${NC}"
        ;;
esac

echo ""
read -p "Press Enter to exit"
