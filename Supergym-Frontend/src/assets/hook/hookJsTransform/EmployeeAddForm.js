import React, { useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";

const EmployeeForm = ({ onSubmit, initialData = {} }) => {
  const [formData, setFormData] = useState({
    userName: initialData.name || "",
    userGender: initialData.gender || "",
    joinDate: initialData.joinDate || "",
    userEmail: initialData.email || "",
    userPhone: initialData.phone || "",
    userEnabled: initialData.status || "",
    role: initialData.role || "",
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData({ ...formData, [name]: value });
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    onSubmit(formData);
  };

  return (
    <div id="employeeModal" className="modal fade">
      <div className="modal-dialog">
        <div className="modal-content">
          <form id="employeeForm" onSubmit={handleSubmit}>
            <div className="modal-header">
              <h4 className="modal-title">Thêm người dùng</h4>
              <button type="button" className="close" data-dismiss="modal" aria-hidden="true">
                &times;
              </button>
            </div>
            <div className="modal-body">
              <div className="form-group">
                <label>Họ tên</label>
                <input type="text" className="form-control" name="userName" value={formData.userName} onChange={handleInputChange} required />
              </div>

              <div className="form-group">
                <label>Giới tính</label>
                <div>
                  <label>
                    <input type="radio" name="userGender" value="male" checked={formData.userGender === "male"} onChange={handleInputChange} /> Nam
                  </label>
                  <label>
                    <input type="radio" name="userGender" value="female" checked={formData.userGender === "female"} onChange={handleInputChange} /> Nữ
                  </label>
                </div>
              </div>

              <div className="form-group">
                <label>Ngày tham gia</label>
                <input type="date" className="form-control" name="joinDate" value={formData.joinDate} onChange={handleInputChange} required />
              </div>

              <div className="form-group">
                <label>Email</label>
                <input type="email" className="form-control" name="userEmail" value={formData.userEmail} onChange={handleInputChange} required />
              </div>

              <div className="form-group">
                <label>Số điện thoại</label>
                <input type="text" className="form-control" name="userPhone" value={formData.userPhone} onChange={handleInputChange} required />
              </div>

              <div className="form-group">
                <label>Trạng thái</label>
                <div>
                  <label>
                    <input type="radio" name="userEnabled" value="active" checked={formData.userEnabled === "active"} onChange={handleInputChange} /> Hoạt động
                  </label>
                  <label>
                    <input type="radio" name="userEnabled" value="inactive" checked={formData.userEnabled === "inactive"} onChange={handleInputChange} /> Không hoạt động
                  </label>
                </div>
              </div>

              <div className="form-group">
                <label>Vai trò</label>
                <div>
                  <label>
                    <input type="radio" name="role" value="staff" checked={formData.role === "staff"} onChange={handleInputChange} /> Nhân viên
                  </label>
                  <label>
                    <input type="radio" name="role" value="admin" checked={formData.role === "admin"} onChange={handleInputChange} /> Admin
                  </label>
                </div>
              </div>
            </div>

            <div className="modal-footer">
              <input type="button" className="btn btn-default" data-dismiss="modal" value="Hủy" />
              <input type="submit" className="btn btn-success" value="Lưu" />
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default EmployeeForm;
