using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace qlsv
{
    public partial class frm_login : Form
    {
        private readonly StudentRepository _repository = new StudentRepository();
        private readonly BindingList<Student> _students = new BindingList<Student>();
        private readonly BindingList<Student> _viewStudents = new BindingList<Student>();
        private readonly BindingSource _bindingSource = new BindingSource();

        public frm_login()
        {
            InitializeComponent();
            ConfigureUi();
            WireEvents();
            _repository.EnsureDatabase();
            LoadStudents();
        }

        private void ConfigureUi()
        {
            if (cbbGioiTinh.Items.Count == 0)
            {
                cbbGioiTinh.Items.AddRange(new object[] { "Nam", "Nữ", "Khác" });
            }

            cbbGioiTinh.SelectedIndex = 0;
            dtpNgaySinh.Format = DateTimePickerFormat.Short;
            dtpNgaySinh.MaxDate = DateTime.Today;

            dgvSinhVien.AutoGenerateColumns = false;
            colMaSV.DataPropertyName = nameof(Student.MaSV);
            colHoTen.DataPropertyName = nameof(Student.HoTen);
            colNgaySinh.DataPropertyName = nameof(Student.NgaySinh);
            colGioiTinh.DataPropertyName = nameof(Student.GioiTinh);
            colLop.DataPropertyName = nameof(Student.Lop);

            _bindingSource.DataSource = _viewStudents;
            dgvSinhVien.DataSource = _bindingSource;
        }

        private void WireEvents()
        {
            btnThem.Click += (s, e) => AddStudent();
            btnCapNhat.Click += (s, e) => UpdateStudent();
            btnSua.Click += (s, e) => LoadSelectedToForm();
            btnXoa.Click += (s, e) => DeleteStudent();
            btnTim.Click += (s, e) => ApplyFilter();
            txtTimKiem.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ApplyFilter();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            dgvSinhVien.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    LoadSelectedToForm();
                }
            };
        }

        private void LoadStudents()
        {
            var students = _repository.GetAll();

            _students.RaiseListChangedEvents = false;
            _students.Clear();
            foreach (var sv in students)
            {
                _students.Add(sv);
            }
            _students.RaiseListChangedEvents = true;

            ApplyFilter();
        }

        private bool TryGetStudentFromForm(out Student student)
        {
            student = null;

            string maSv = txtMaSV.Text.Trim();
            string hoTen = txtHoTen.Text.Trim();
            string lop = txtLopQL.Text.Trim();
            string gioiTinh = cbbGioiTinh.SelectedItem?.ToString() ?? string.Empty;
            DateTime ngaySinh = dtpNgaySinh.Value.Date;

            if (string.IsNullOrWhiteSpace(maSv) || string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(lop))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Mã SV, Họ tên và Lớp quản lý.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            student = new Student
            {
                MaSV = maSv,
                HoTen = hoTen,
                NgaySinh = ngaySinh,
                GioiTinh = string.IsNullOrWhiteSpace(gioiTinh) ? "Nam" : gioiTinh,
                Lop = lop
            };

            return true;
        }

        private void AddStudent()
        {
            if (!TryGetStudentFromForm(out var student))
            {
                return;
            }

            try
            {
                _repository.Insert(student);
            }
            catch (SqlException ex) when (IsDuplicateKey(ex))
            {
                MessageBox.Show("Mã sinh viên đã tồn tại.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không lưu được dữ liệu. Chi tiết: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadStudents();
            ClearForm();
        }

        private void UpdateStudent()
        {
            var selected = GetSelectedStudent();
            if (selected == null)
            {
                MessageBox.Show("Chọn một sinh viên trong danh sách để cập nhật.", "Chưa chọn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryGetStudentFromForm(out var updated))
            {
                return;
            }

            try
            {
                _repository.Update(selected.MaSV, updated);
            }
            catch (SqlException ex) when (IsDuplicateKey(ex))
            {
                MessageBox.Show("Mã sinh viên đã tồn tại cho người khác.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không cập nhật được. Chi tiết: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadStudents();
            ClearForm();
        }

        private void LoadSelectedToForm()
        {
            var selected = GetSelectedStudent();
            if (selected == null)
            {
                MessageBox.Show("Chọn một sinh viên trong danh sách để tải dữ liệu chỉnh sửa.", "Chưa chọn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            txtMaSV.Text = selected.MaSV;
            txtHoTen.Text = selected.HoTen;
            dtpNgaySinh.Value = selected.NgaySinh;
            cbbGioiTinh.SelectedItem = selected.GioiTinh;
            txtLopQL.Text = selected.Lop;
        }

        private void DeleteStudent()
        {
            var selected = GetSelectedStudent();
            if (selected == null)
            {
                MessageBox.Show("Chọn một sinh viên để xóa.", "Chưa chọn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show($"Bạn có chắc muốn xóa sinh viên {selected.HoTen}?", "Xóa sinh viên", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _repository.Delete(selected.MaSV);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không xóa được. Chi tiết: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            LoadStudents();
            ClearForm();
        }

        private void ApplyFilter()
        {
            string keyword = txtTimKiem.Text.Trim().ToLowerInvariant();
            var filtered = _students.Where(s =>
                string.IsNullOrEmpty(keyword) ||
                s.MaSV.ToLowerInvariant().Contains(keyword) ||
                s.HoTen.ToLowerInvariant().Contains(keyword) ||
                s.Lop.ToLowerInvariant().Contains(keyword)).ToList();

            _viewStudents.RaiseListChangedEvents = false;
            _viewStudents.Clear();
            foreach (var sv in filtered)
            {
                _viewStudents.Add(sv);
            }
            _viewStudents.RaiseListChangedEvents = true;
            _bindingSource.ResetBindings(false);
        }

        private Student GetSelectedStudent()
        {
            return dgvSinhVien.CurrentRow?.DataBoundItem as Student;
        }

        private void ClearForm()
        {
            txtMaSV.Clear();
            txtHoTen.Clear();
            txtLopQL.Clear();
            cbbGioiTinh.SelectedIndex = 0;
            dtpNgaySinh.Value = DateTime.Today;
            txtMaSV.Focus();
        }

        private static bool IsDuplicateKey(SqlException ex)
        {
            return ex.Number == 2627 || ex.Number == 2601;
        }
    }
}
