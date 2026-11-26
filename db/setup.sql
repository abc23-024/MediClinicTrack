-- Run once in MySQL as root. Replace YOUR_PASSWORD with your own password
-- (and use the same one in your user-secrets connection string).
CREATE DATABASE IF NOT EXISTS mediclinictrack;
CREATE USER IF NOT EXISTS 'mct_app'@'localhost' IDENTIFIED BY 'YOUR_PASSWORD';
GRANT ALL PRIVILEGES ON mediclinictrack.* TO 'mct_app'@'localhost';
FLUSH PRIVILEGES;
