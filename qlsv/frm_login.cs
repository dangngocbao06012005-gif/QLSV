using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace qlsv
{
    public partial class frm_login : Form
    {
        private readonly BindingList<Student> _students = new BindingList<Student>();
        private readonly BindingList<Student> _viewStudents = new BindingList<Student>();
        private readonly BindingSource _bindingSource = new BindingSource();

        public frm_login()
        {
            InitializeComponent();
            ConfigureUi();
            WireEvents();
            ApplyFilter();
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

            if (_students.Any(s => s.MaSV.Equals(student.MaSV, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Mã sinh viên đã tồn tại.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _students.Add(student);
            ApplyFilter();
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

            bool duplicateCode = _students.Any(s => !ReferenceEquals(s, selected) && s.MaSV.Equals(updated.MaSV, StringComparison.OrdinalIgnoreCase));
            if (duplicateCode)
            {
                MessageBox.Show("Mã sinh viên đã tồn tại cho người khác.", "Trùng mã", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            selected.MaSV = updated.MaSV;
            selected.HoTen = updated.HoTen;
            selected.NgaySinh = updated.NgaySinh;
            selected.GioiTinh = updated.GioiTinh;
            selected.Lop = updated.Lop;

            _bindingSource.ResetBindings(false);
            ApplyFilter();
            ClearForm();
        }

        private void LoadSelectedToForm()
        {
            var selected = GetSelectedStudent();
            if (selected == null)
            {
                MessageBox.Show("Chọn một sinh viên để tải dữ liệu chỉnh sửa.", "Chưa chọn dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            _students.Remove(selected);
            ApplyFilter();
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

        private class Student
        {
            public string MaSV { get; set; }
            public string HoTen { get; set; }
            public DateTime NgaySinh { get; set; }
            public string GioiTinh { get; set; }
            public string Lop { get; set; }
        }
    }
}
