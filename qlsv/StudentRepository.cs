using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

namespace qlsv
{
    public class StudentRepository
    {
        private readonly string _connectionString;

        public StudentRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["QLSV"]?.ConnectionString
                ?? throw new InvalidOperationException("Thiếu connectionString 'QLSV' trong App.config.");
        }

        public void EnsureDatabase()
        {
            var dataDir = AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString()
                          ?? AppDomain.CurrentDomain.BaseDirectory;
            var mdfPath = Path.Combine(dataDir, "qlsv.mdf");
            var ldfPath = Path.Combine(dataDir, "qlsv_log.ldf");

            if (!File.Exists(mdfPath))
            {
                var masterConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;Connect Timeout=30;Initial Catalog=master";
                var createDbSql = $@"
IF DB_ID('qlsv_localdb') IS NULL
BEGIN
    CREATE DATABASE [qlsv_localdb]
    ON PRIMARY (NAME = N'qlsv_localdb', FILENAME = '{mdfPath.Replace("'", "''")}')
    LOG ON (NAME = N'qlsv_localdb_log', FILENAME = '{ldfPath.Replace("'", "''")}')
END";

                using (var connection = new SqlConnection(masterConnectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(createDbSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var createTableSql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Students')
BEGIN
    CREATE TABLE Students
    (
        MaSV NVARCHAR(20) NOT NULL PRIMARY KEY,
        HoTen NVARCHAR(255) NOT NULL,
        NgaySinh DATE NOT NULL,
        GioiTinh NVARCHAR(10) NOT NULL,
        Lop NVARCHAR(50) NOT NULL
    );
END";
                using (var command = new SqlCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public IList<Student> GetAll()
        {
            var result = new List<Student>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("SELECT MaSV, HoTen, NgaySinh, GioiTinh, Lop FROM Students ORDER BY MaSV", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new Student
                        {
                            MaSV = reader.GetString(0),
                            HoTen = reader.GetString(1),
                            NgaySinh = reader.GetDateTime(2),
                            GioiTinh = reader.GetString(3),
                            Lop = reader.GetString(4)
                        });
                    }
                }
            }

            return result;
        }

        public void Insert(Student student)
        {
            const string sql = @"INSERT INTO Students (MaSV, HoTen, NgaySinh, GioiTinh, Lop)
                                 VALUES (@MaSV, @HoTen, @NgaySinh, @GioiTinh, @Lop)";
            ExecuteNonQuery(sql, student);
        }

        public void Update(string originalMaSv, Student student)
        {
            const string sql = @"UPDATE Students
                                 SET MaSV = @NewMaSV,
                                     HoTen = @HoTen,
                                     NgaySinh = @NgaySinh,
                                     GioiTinh = @GioiTinh,
                                     Lop = @Lop
                                 WHERE MaSV = @OriginalMaSV";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@NewMaSV", student.MaSV);
                command.Parameters.AddWithValue("@HoTen", student.HoTen);
                command.Parameters.AddWithValue("@NgaySinh", student.NgaySinh);
                command.Parameters.AddWithValue("@GioiTinh", student.GioiTinh);
                command.Parameters.AddWithValue("@Lop", student.Lop);
                command.Parameters.AddWithValue("@OriginalMaSV", originalMaSv);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public void Delete(string maSv)
        {
            const string sql = @"DELETE FROM Students WHERE MaSV = @MaSV";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@MaSV", maSv);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private void ExecuteNonQuery(string sql, Student student)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@MaSV", student.MaSV);
                command.Parameters.AddWithValue("@HoTen", student.HoTen);
                command.Parameters.AddWithValue("@NgaySinh", student.NgaySinh);
                command.Parameters.AddWithValue("@GioiTinh", student.GioiTinh);
                command.Parameters.AddWithValue("@Lop", student.Lop);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
