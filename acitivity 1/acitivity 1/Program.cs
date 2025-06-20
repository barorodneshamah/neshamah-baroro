


using System;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

class Program
{
    static string connectionString = "server=localhost;database=dbmsconnection;user=root;";

    static void Main()
    {
        try
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                DisplayMenu();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    static void DisplayMenu()
    {
        while (true)
        {
            Console.WriteLine("\n╔═══════════════════════════════════╗");
            Console.WriteLine("║     SCHOOL NORMALIZATION MENU     ║");
            Console.WriteLine("╠═══════════════════════════════════╣");
            Console.WriteLine("║ 1. Insert Multiple Raw Data       ║");
            Console.WriteLine("║ 2. View Raw Data                  ║");
            Console.WriteLine("║ 3. Display 1st Normal Form        ║");
            Console.WriteLine("║ 4. Display 2nd Normal Form        ║");
            Console.WriteLine("║ 5. Display 3rd Normal Form        ║");
            Console.WriteLine("║ 6. Delete Individual Record       ║");
            Console.WriteLine("║ 7. Delete All Records             ║");
            Console.WriteLine("║ 8. Exit                           ║");
            Console.WriteLine("╚═══════════════════════════════════╝");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    while (!InsertSchoolData()) { }
                    break;
                case "2":
                    FetchAndDisplayRawData();
                    break;
                case "3":
                    FetchAndDisplay1NF();
                    break;
                case "4":
                    FetchAndDisplay2NF();
                    break;
                case "5":
                    FetchAndDisplay3NF();
                    break;
                case "6":
                    DeleteIndividualRecord();
                    break;
                case "7":
                    DeleteAllRecords();
                    break;
                case "8":
                    Console.WriteLine("\nExiting program.");
                    return;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    break;
            }
        }
    }

    static bool InsertSchoolData()
    {
        Console.Write("\nEnter Department Name: ");
        string departmentName = Console.ReadLine();

        Console.Write("\nEnter Student Names (comma-separated): ");
        string studentInput = Console.ReadLine();

        Console.Write("Enter Course Names (comma-separated): ");
        string courseInput = Console.ReadLine();

        Console.Write("Enter Instructor Names (comma-separated): ");
        string instructorInput = Console.ReadLine();

        string[] students = studentInput.Split(',').Select(e => e.Trim()).ToArray();
        string[] courses = courseInput.Split(',').Select(e => e.Trim()).ToArray();
        string[] instructors = instructorInput.Split(',').Select(e => e.Trim()).ToArray();

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();

                // Insert new department
                int departmentId;
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO department (department_name) VALUES (@DepartmentName); SELECT LAST_INSERT_ID();", conn))
                {
                    cmd.Parameters.AddWithValue("@DepartmentName", departmentName);
                    departmentId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // Insert students, courses, and instructors
                foreach (var student in students)
                {
                    int studentId;
                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO students (student_name, department_id) VALUES (@StudentName, @DepartmentId); SELECT LAST_INSERT_ID();", conn))
                    {
                        cmd.Parameters.AddWithValue("@StudentName", student);
                        cmd.Parameters.AddWithValue("@DepartmentId", departmentId);
                        studentId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (var course in courses)
                    {
                        int courseId;
                        using (MySqlCommand cmd = new MySqlCommand("INSERT INTO courses (student_id, course_name) VALUES (@StudentId, @CourseName); SELECT LAST_INSERT_ID();", conn))
                        {
                            cmd.Parameters.AddWithValue("@StudentId", studentId);
                            cmd.Parameters.AddWithValue("@CourseName", course);
                            courseId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (var instructor in instructors)
                        {
                            using (MySqlCommand cmd = new MySqlCommand("INSERT INTO instructors (course_id, instructor_name) VALUES (@CourseId, @InstructorName);", conn))
                            {
                                cmd.Parameters.AddWithValue("@CourseId", courseId);
                                cmd.Parameters.AddWithValue("@InstructorName", instructor);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                Console.WriteLine("School data saved successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting data: " + ex.Message);
                return false;
            }
        }
    }

    static void FetchAndDisplayRawData()
    {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            Console.WriteLine("RAW DATA TABLE:");
            conn.Open();
            string query = @"
        SELECT 
            d.department_id, -- Adding department ID
            d.department_name,
            GROUP_CONCAT(DISTINCT s.student_name ORDER BY s.student_id) AS students,
            GROUP_CONCAT(DISTINCT c.course_name ORDER BY c.course_id) AS courses,
            GROUP_CONCAT(DISTINCT i.instructor_name ORDER BY i.instructor_id) AS instructors
        FROM 
            department d
        JOIN
            students s ON d.department_id = s.department_id
        LEFT JOIN 
            courses c ON s.student_id = c.student_id
        LEFT JOIN 
            instructors i ON c.course_id = i.course_id
        GROUP BY 
            d.department_id
        ORDER BY 
            d.department_id;";

            ExecuteQuery(query);
        }
    }

    static void FetchAndDisplay1NF()
    {
        Console.WriteLine("1NF TABLE:");
        string query = @"
WITH RECURSIVE department_split AS (
    SELECT 
        department_id, 
        TRIM(SUBSTRING_INDEX(department_name, ',', 1)) AS department_name, 
        NULLIF(SUBSTRING(department_name FROM LENGTH(SUBSTRING_INDEX(department_name, ',', 1)) + 2), '') AS remaining,
        1 AS dept_rank
    FROM department
    UNION ALL
    SELECT 
        department_id, 
        TRIM(SUBSTRING_INDEX(remaining, ',', 1)) AS department_name, 
        NULLIF(SUBSTRING(remaining FROM LENGTH(SUBSTRING_INDEX(remaining, ',', 1)) + 2), '') AS remaining,
        dept_rank + 1
    FROM department_split
    WHERE remaining IS NOT NULL AND dept_rank < 2  -- Limit to only 2 department values per student
)

SELECT DISTINCT
    s.student_id,
    s.student_name,
    ds.department_id,
    ds.department_name,
    c.course_name, 
    i.instructor_name
FROM students s
JOIN department_split ds ON FIND_IN_SET(ds.department_id, s.department_id)
LEFT JOIN courses c ON s.student_id = c.student_id
LEFT JOIN instructors i ON c.course_id = i.course_id
ORDER BY s.student_id, ds.department_name, c.course_name, i.instructor_name;";

        ExecuteQuery(query);
    }




    static void FetchAndDisplay2NF()
    {
        Console.WriteLine("2NF TABLE:");

        // Course Table
        Console.WriteLine("COURSE TABLE:");
        string courseQuery = @"
    SELECT 
        MIN(c.course_id) AS CourseID,  -- Select the minimum CourseID for each unique course_name
        c.course_name AS CourseName
    FROM 
        courses c
    GROUP BY 
        c.course_name  -- Group by CourseName to remove duplicates
    ORDER BY 
        CourseID;";

        ExecuteQuery(courseQuery);

        // Instructor Table
        Console.WriteLine("\nINSTRUCTOR TABLE:");
        string instructorQuery = @"
    SELECT 
        MIN(i.instructor_id) AS InstructorID,  -- Select the minimum InstructorID for each unique instructor_name            
        i.instructor_name AS InstructorName
    FROM 
        instructors i
    GROUP BY 
        i.instructor_name  -- Group by InstructorName to remove duplicates
    ORDER BY 
        InstructorID;";

        ExecuteQuery(instructorQuery);
    }








    static void FetchAndDisplay3NF()
    {
        Console.WriteLine("3NF TABLE:");

        Console.WriteLine("\nSTUDENTS TABLE:");
        ExecuteQuery("SELECT student_id, student_name FROM students;");

        Console.WriteLine("\nCOURSES TABLE:");
        ExecuteQuery("SELECT course_id, student_id, course_name FROM courses;");

        Console.WriteLine("\nINSTRUCTORS TABLE:");
        ExecuteQuery("SELECT instructor_id, course_id, instructor_name FROM instructors;");
    }

    static void DeleteIndividualRecord()
    {
        Console.Write("Enter Student ID to delete: ");
        int studentId = int.Parse(Console.ReadLine());

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM students WHERE student_id = @StudentId;", conn))
                {
                    cmd.Parameters.AddWithValue("@StudentId", studentId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Record deleted successfully.");
                    }
                    else
                    {
                        Console.WriteLine("No record found with that Student ID.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting record: " + ex.Message);
            }
        }
    }

    static void DeleteAllRecords()
    {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();

                // Delete from instructors first
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM instructors;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Reset AUTO_INCREMENT for instructors
                using (MySqlCommand cmd = new MySqlCommand("ALTER TABLE instructors AUTO_INCREMENT = 1;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Then delete from courses
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM courses;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Reset AUTO_INCREMENT for courses
                using (MySqlCommand cmd = new MySqlCommand("ALTER TABLE courses AUTO_INCREMENT = 1;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Then delete from students
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM students;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Reset AUTO_INCREMENT for students
                using (MySqlCommand cmd = new MySqlCommand("ALTER TABLE students AUTO_INCREMENT = 1;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Finally delete from department
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM department;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Reset AUTO_INCREMENT for department
                using (MySqlCommand cmd = new MySqlCommand("ALTER TABLE department AUTO_INCREMENT = 1;", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                Console.WriteLine("All records deleted and AUTO_INCREMENT values reset successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting records: " + ex.Message);
            }
        }
    }


    static void ExecuteQuery(string query)
    {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            conn.Open();
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                var columnNames = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
                var columnWidths = columnNames.Select(name => name.Length).ToArray();
                var rows = new List<string[]>();

                while (reader.Read())
                {
                    var row = new string[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString();
                        row[i] = value;
                        if (value.Length > columnWidths[i]) columnWidths[i] = value.Length;
                    }
                    rows.Add(row);
                }

                // Pass a table title for the PrintTable method
                PrintTable(columnNames, columnWidths, rows, "Query Results");
            }
        }
    }


    static void PrintTable(string[] columnNames, int[] columnWidths, List<string[]> rows, string tableTitle)
    {
        // Calculate the total width of the table
        int totalWidth = columnWidths.Sum() + columnNames.Length * 3 + 1; // Extra space for padding and borders

        // Center the title
        int titleLength = tableTitle.Length;
        int paddingLeft = (totalWidth - titleLength - 2) / 2; // Calculate left padding
        int paddingRight = totalWidth - titleLength - paddingLeft - 2; // Calculate right padding

        // Print the table title with a dynamic border
        string titleBorder = "═".PadRight(totalWidth - 2, '═');
        string titleLine = $"╔{titleBorder}╗";
        Console.WriteLine(titleLine);
        Console.WriteLine($"║{new string(' ', paddingLeft)}{tableTitle}{new string(' ', paddingRight)}║");
        Console.WriteLine($"╠{new string('═', totalWidth - 2)}╣");

        // Print column headers
        Console.Write("║");
        for (int i = 0; i < columnNames.Length; i++)
        {
            Console.Write($" {columnNames[i].PadRight(columnWidths[i])} ║");
        }
        Console.WriteLine();

        // Print the separator
        Console.WriteLine($"╠{new string('═', totalWidth - 2)}╣");

        // Print each row
        foreach (var row in rows)
        {
            Console.Write("║");
            for (int i = 0; i < row.Length; i++)
            {
                Console.Write($" {row[i].PadRight(columnWidths[i])} ║");
            }
            Console.WriteLine();
        }

        // Print the closing border
        Console.WriteLine($"╚{new string('═', totalWidth - 2)}╝");
    }
}