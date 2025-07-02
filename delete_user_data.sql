-- SQL functions to delete user data when deleting an account

-- Function to delete all recipes created by a user
CREATE OR REPLACE FUNCTION delete_user_recipes(user_id UUID) 
RETURNS void AS $$
BEGIN
    -- Delete all recipes where the user is the creator
    DELETE FROM recipes WHERE user_id = $1;
END;
$$ LANGUAGE plpgsql;

-- Function to delete all saved recipes for a user
CREATE OR REPLACE FUNCTION delete_user_saved_recipes(user_id UUID) 
RETURNS void AS $$
BEGIN
    -- Delete all saved recipes for the user
    DELETE FROM saved_recipes WHERE user_id = $1;
END;
$$ LANGUAGE plpgsql;

-- Note: These functions should be executed in the Supabase SQL Editor
-- before the delete account functionality is used 