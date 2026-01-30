-- Drop existing FK
ALTER TABLE locations DROP CONSTRAINT IF EXISTS locations_user_id_fkey;

-- Create new FK to users.user_id instead of users.id
ALTER TABLE locations ADD CONSTRAINT locations_user_id_fkey FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE;
