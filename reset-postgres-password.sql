-- Run this after setting pg_hba.conf to 'trust' mode
-- psql -U postgres -f reset-postgres-password.sql

ALTER USER postgres WITH PASSWORD 'GarantDockerPass';

-- Verify the change
\du postgres
