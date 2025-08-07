#!/bin/sh

until pg_isready -h db -U postgres; do
  echo "Waiting for PostgreSQL to start..."
  sleep 1
done

echo "Applying database migrations..."
dotnet ef database update --project FitnessTrackerBack

echo "Starting application..."
exec dotnet watch --project FitnessTrackerBack run --urls "http://0.0.0.0:5024"
