#!/bin/bash

# Wait for backend to be ready
echo "Waiting for backend to start..."
sleep 10

# Run the scan
echo "Starting hard drive scan..."
curl -X POST http://localhost:5158/api/books/scan-specific-folder \
  -H "Content-Type: application/json" \
  -d '{"folderPath": "/mnt/books/BooksCollection/IslamicSites"}'

echo ""
echo "Scan completed!"
