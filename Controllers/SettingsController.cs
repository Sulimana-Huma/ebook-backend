using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using ebook_backend_api.Models;
using System.Collections.Generic;

namespace ebook_backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly string _connectionString =
            "server=localhost;database=ebook_system;user=ebook_system;password=eBook@2025;";

        // POST: save a new folder path
        [HttpPost("save-path")]
        public IActionResult SavePath([FromBody] BooksFolderSetting model)
        {
            if (string.IsNullOrEmpty(model.Path))
                return BadRequest("Folder path cannot be empty");

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // Check if path already exists
                var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM books_folder_settings WHERE folder_path = @path", conn);
                checkCmd.Parameters.AddWithValue("@path", model.Path);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (exists)
                    return BadRequest("Path already exists");

                // Insert new path
                var insertCmd = new MySqlCommand(
                    "INSERT INTO books_folder_settings (folder_path, updated_at) VALUES (@path, NOW())", conn);
                insertCmd.Parameters.AddWithValue("@path", model.Path);
                insertCmd.ExecuteNonQuery();
            }

            return Ok(new { message = "Path saved successfully" });
        }

        // GET: get all saved paths
        [HttpGet("get-paths")]
        public IActionResult GetPaths()
        {
            var paths = new List<BooksFolderSetting>();
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();
                var query = "SELECT id, folder_path FROM books_folder_settings ORDER BY updated_at DESC";
                var cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    paths.Add(new BooksFolderSetting
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Path = reader["folder_path"].ToString()
                    });
                }
            }

            return Ok(paths); // Return directly, don't map again
        }

        // PUT: update an existing path
        [HttpPut("update-path/{id}")]
        public IActionResult UpdatePath(int id, [FromBody] BooksFolderSetting model)
        {
            if (string.IsNullOrEmpty(model.Path))
                return BadRequest("Folder path cannot be empty");

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // Check if path exists
                var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM books_folder_settings WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (!exists)
                    return NotFound("Path not found");

                // Check if new path already exists (excluding current record)
                var duplicateCheck = new MySqlCommand(
                    "SELECT COUNT(*) FROM books_folder_settings WHERE folder_path = @path AND id != @id", conn);
                duplicateCheck.Parameters.AddWithValue("@path", model.Path);
                duplicateCheck.Parameters.AddWithValue("@id", id);
                var duplicate = Convert.ToInt32(duplicateCheck.ExecuteScalar()) > 0;

                if (duplicate)
                    return BadRequest("Path already exists");

                var query = "UPDATE books_folder_settings SET folder_path=@path, updated_at=NOW() WHERE id=@id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@path", model.Path);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "Path updated successfully" });
        }

        // DELETE: delete a path
        [HttpDelete("delete-path/{id}")]
        public IActionResult DeletePath(int id)
        {
            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Open();

                // Check if path exists
                var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM books_folder_settings WHERE id = @id", conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;

                if (!exists)
                    return NotFound("Path not found");

                var query = "DELETE FROM books_folder_settings WHERE id=@id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "Path deleted successfully" });
        }
    }
}
