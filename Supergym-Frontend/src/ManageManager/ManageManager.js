import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./ManageManager.css";

import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import { Delete } from "@mui/icons-material";

const ManageCustomer = () => {
  const [StaffDataList, setStaffDataList] = useState([]);
  //PRELOAD
  const [StaffData, setStaffData] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [isTimeoutFinished, setIsTimeoutFinished] = useState(false);
  //END PRELOAD

  const [currentStaff, setCurrentStaff] = useState(null); // for editing Staff
  const [StaffToDelete, setStaffToDelete] = useState(null); // for deletion
  const [formData, setFormData] = useState({
    name: "",
    gender: "male",
    dob: "",
    email: "",
    phone: "",
    // userEnabled: "active",
    // role: "staff",
  });
  const [errors, setErrors] = useState({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 2; // Maximum 2 employees per page

  const StaffModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);

  //FETCH DATA AND PRELOAD
  useEffect(() => {
    const fetchStaffs = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Users/staffs", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setStaffData(response.data);
      } catch (error) {
        console.error("Error fetching customers:", error);
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchStaffs();
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setIsTimeoutFinished(true); // Timeout finished
    }, 500);

    return () => clearTimeout(timer); // Clear timeout on unmount
  }, []);

  const isLoading = isDataLoading || !isTimeoutFinished;
  //END FETCH DATA AND PRELOAD

  //PAGENATION
  const totalPages = Math.ceil(StaffData.length / itemsPerPage);
  const currentStaffs = StaffData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);
  const formatDate = (dob) => `${dob.date}/${dob.month}/${dob.year}`;
  const handlePageChange = (pageNumber) => {
    setCurrentPage(pageNumber);
  };
  //END PAGENATION

  const openAddStaffModal = () => {
    setFormData({
      name: "",
      gender: "male",
      dob: "",
      email: "",
      phone: "",
      userEnabled: "active",
      role: "staff",
    });
    setCurrentStaff(null); // Adding a new Staff
    StaffModalRef.current.style.display = "block"; // Hiển thị modal
    StaffModalRef.current.classList.add("active"); // Thêm class 'active'
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openEditStaffModal = (Staff) => {
    // Convert DOB object to a date string compatible with the input field
    const formattedDob = `${Staff.dob.year}-${String(Staff.dob.month).padStart(2, "0")}-${String(Staff.dob.date).padStart(2, "0")}`;

    setFormData({
      name: Staff.name,
      gender: Staff.gender.toLowerCase() === "nam" ? "male" : "female",
      dob: formattedDob, // Set formatted date
      email: Staff.email,
      phone: Staff.phone,
      address: Staff.address,
    });
    setCurrentStaff(Staff); // Set to editing mode by assigning the Staff
    StaffModalRef.current.style.display = "block";
    StaffModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeModal = () => {
    StaffModalRef.current.style.display = "none";
    StaffModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentStaff(null); // Reset current Staff to null when closing modal
  };

  const handleInputChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value,
    });
  };

  const validateForm = () => {
    const newErrors = {};
    if (formData.name.length < 5 || formData.name.length > 30) {
      newErrors.name = "Họ tên phải từ 5 đến 30 ký tự.";
    }

    const dob = new Date(formData.dob);
    const minDate = new Date("2022-03-10");
    const maxDate = new Date("2024-12-12");
    if (dob < minDate || dob > maxDate) {
      newErrors.dob = "Ngày tham gia phải trong khoảng từ 10/03/2022 đến 12/12/2024.";
    }

    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailPattern.test(formData.email)) {
      newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
    }

    if (formData.phone.length < 9 || formData.phone.length > 11 || !/^\d+$/.test(formData.phone)) {
      newErrors.phone = "Số điện thoại phải từ 9 đến 11 chữ số.";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // Return true if no errors
  };

  // Hàm để mở modal thông báo
  const showSuccessModal = () => {
    successModalRef.current.style.display = "block"; // Hiển thị modal
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  // Hàm để đóng modal thông báo
  const closeSuccessModal = () => {
    successModalRef.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
  };

  const openDeleteModal = (Staff) => {
    setStaffToDelete(Staff); // Set Staff to be deleted
    deleteModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeDeleteModal = () => {
    deleteModalRef.current.style.display = "none"; // Hide delete modal
    document.querySelector(".modal-overlay").style.display = "none"; // Hide overlay
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (validateForm()) {
      const token = localStorage.getItem("token");

      // Format date to the required format
      const dob = new Date(formData.dob);
      const dobData = {
        date: dob.getDate(),
        month: dob.getMonth() + 1,
        year: dob.getFullYear(),
      };

      // Create a basic Staff data object
      const StaffData = {
        userId: currentStaff ? currentStaff.userId : "string",
        email: formData.email,
        name: formData.name,
        gender: formData.gender,
        dob: dobData,
        address: formData.address,
        phone: formData.phone,
        roleId: "string",
        userAvatar: "string",
        idCard: "string",
      };

      // Include password only for adding a new Staff
      if (!currentStaff) {
        StaffData.password = formData.password;
      }

      try {
        if (currentStaff) {
          // Update Staff
          await axios.put(`http://localhost:5000/api/Users/updateStaff/${currentStaff.userId}`, StaffData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          // Update Staff data in the list
          setStaffDataList((prevData) => prevData.map((Staff) => (Staff.userId === currentStaff.userId ? { ...Staff, ...StaffData } : Staff)));
        } else {
          // Add new Staff
          const response = await axios.post("http://localhost:5000/api/Users/addStaff", StaffData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          setStaffDataList([...StaffDataList, response.data]);
        }

        // Reset form and close modal
        setFormData({
          email: "",
          password: "",
          name: "",
          gender: "",
          dob: "",
          address: "",
          phone: "",
        });
        showSuccessModal();
        closeModal();
      } catch (error) {
        console.error("Error saving Staff:", error);
      }
    }
  };

  const deleteStaff = async () => {
    if (StaffToDelete) {
      const token = localStorage.getItem("token");
      try {
        await axios.delete(`http://localhost:5000/api/users/${StaffToDelete.userId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        // Xóa nhân viên từ danh sách
        setStaffData(StaffData.filter((cus) => cus.id !== StaffToDelete.id));

        // Ẩn modal và reset nhân viên cần xóa
        closeDeleteModal();
      } catch (error) {
        console.error("Error deleting Staff:", error);
      }
    }
  };
  const handleDeleteStaff = (e) => {
    e.preventDefault();
    deleteStaff();
  };

  return (
    <>
      <Header />

      {/* <!-- ***** Preloader Start ***** --> */}
      {/* {isLoading ? (
        <div id="js-preloader" className="js-preloader">
          <div className="preloader-inner">
            <span className="dot"></span>
            <div className="dots">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>
        </div>
      ) : (
        <div></div>
      )} */}

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageCustomer */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}

      <div className="user-select">
        <h1>Quản lí nhân viên</h1>
        <h2>Người dùng trong hệ thống SUPER GYM</h2>

        <div className="select-search-container">
          <div className="search-container">
            <input type="text" id="searchUser" className="form-control" placeholder="Tìm kiếm..." />
            <span className="search-icon">
              <SearchIcon />
            </span>
          </div>

          <select className="form-control  form-select" id="selectRole">
            <option value="">Chọn vai trò</option>
            <option value="admin">Admin</option>
            <option value="staff">Nhân viên</option>
          </select>

          <select className="form-control form-select" id="selectStatus">
            <option value="">Chọn trạng thái</option>
            <option value="active">Hoạt động</option>
            <option value="inactive">Không hoạt động</option>
          </select>

          <select className="form-control form-select" id="selectGender">
            <option value="">Chọn giới tính</option>
            <option value="male">Nam</option>
            <option value="female">Nữ</option>
          </select>
        </div>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Quản lí nhân viên</h2>
              </div>
              <div className="col-sm-6">
                <button onClick={openAddStaffModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm mới nhân viên</span>
                </button>
              </div>
            </div>
          </div>

          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                <th className="name-el">Họ tên</th>
                <th>Giới tính</th>
                <th>Ngày sinh</th>
                <th>Email</th>
                <th>Số điện thoại</th>
                <th>Địa chỉ</th>
                {/* <th className="status-center">Trạng thái</th> */}
                {/* <th className="role-el">Vai trò</th> */}
                <th className="action-el">Hành động</th>
              </tr>
            </thead>
            <tbody>
              {currentStaffs.map((Staff, index) => (
                <tr key={index}>
                  <td>
                    <img src={`data:image/jpeg;base64,${Staff.userAvatar}`} className="Staff-avatar" />
                    {Staff.name}
                  </td>
                  {/* <td>{Staff.gender}</td> */}
                  <td>{Staff.gender === "Male" ? "Nam" : "Nữ"}</td>
                  <td>{formatDate(Staff.dob)}</td>
                  <td>{Staff.email}</td>
                  <td>{Staff.phone}</td>
                  <td>{Staff.address}</td>
                  {/* <td className={Staff.status === "Hoạt động" ? "status-el-active" : "status-el-inactive"}>{Staff.status}</td> */}
                  {/* <td>{Staff.role}</td> */}
                  <td>
                    <a href="#" onClick={() => openEditStaffModal(Staff)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openDeleteModal(Staff)} className="delete">
                      <DeleteIcon />
                    </a>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Pagination */}
          <div className="clearfix-el">
            <div className="hint-text">
              Showing <b>{currentStaffs.length}</b> out of <b>{StaffData.length}</b> entries
            </div>
            <ul className="pagination">
              <li className={`page-item ${currentPage === 1 ? "disabled" : ""}`}>
                <a href="#" onClick={() => handlePageChange(currentPage - 1)} className="page-link">
                  <ChevronLeftIcon />
                </a>
              </li>
              {Array.from({ length: totalPages }, (_, index) => (
                <li key={index} className={`page-item ${currentPage === index + 1 ? "active" : ""}`}>
                  <a href="#" onClick={() => handlePageChange(index + 1)} className="page-link">
                    {index + 1}
                  </a>
                </li>
              ))}
              <li className={`page-item ${currentPage === totalPages ? "disabled" : ""}`}>
                <a href="#" onClick={() => handlePageChange(currentPage + 1)} className="page-link">
                  <ChevronRightIcon />
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* Staff Modal */}
      <div ref={StaffModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="employeeForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentStaff ? "Sửa thông tin người dùng" : "Thêm nhân viên"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>Họ tên</label>
                    <input type="text" className="form-control" name="name" value={formData.name} onChange={handleInputChange} required />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>

                  <div className="form-group col">
                    <label>Ngày tham gia</label>
                    <input type="date" className="form-control" name="dob" value={formData.dob} onChange={handleInputChange} required />
                    {errors.dob && <div className="error-message">{errors.dob}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Email</label>
                    <input type="email" className="form-control" name="email" value={formData.email} onChange={handleInputChange} required />
                    {errors.email && <div className="error-message">{errors.email}</div>}
                  </div>

                  <div className="form-group col">
                    <label>Số điện thoại</label>
                    <input type="text" className="form-control" name="phone" value={formData.phone} onChange={handleInputChange} required />
                    {errors.phone && <div className="error-message">{errors.phone}</div>}
                  </div>
                </div>

                {/* Conditionally render the password input only if adding a new Staff */}
                {!currentStaff && (
                  <div className="row">
                    <div className="form-group col">
                      <label>Mật khẩu</label>
                      <input type="password" className="form-control" name="password" value={formData.password} onChange={handleInputChange} required />
                    </div>
                  </div>
                )}

                <div className="row">
                  <div className="form-group col">
                    <label>Địa chỉ</label>
                    <input type="text" className="form-control" name="address" value={formData.address} onChange={handleInputChange} required />
                  </div>
                </div>

                {/* Gender radio buttons */}
                <div className="row">
                  <div className="form-group col">
                    <label>Giới tính</label>
                    <div className="radio-group">
                      <label>
                        <input type="radio" name="gender" value="male" checked={formData.gender === "male"} onChange={handleInputChange} />
                        Nam
                      </label>
                      <label>
                        <input type="radio" name="gender" value="female" checked={formData.gender === "female"} onChange={handleInputChange} />
                        Nữ
                      </label>
                    </div>
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal}>
                  Hủy
                </button>
                <button type="submit" className="btn btn-success">
                  Lưu
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
      {/* Modal Overlay */}
      <div className="modal-overlay"></div>

      {/* Delete Modal */}
      <div ref={deleteModalRef} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <form id="deleteEmployeeForm" onSubmit={handleDeleteStaff}>
              <div className="modal-header">
                <h4 className="modal-title">Xóa người dùng</h4>
                <button type="button" className="close" onClick={closeDeleteModal}>
                  &times;
                </button>
              </div>
              <div className="modal-body">
                <p>Bạn có chắc chắn muốn xóa {StaffToDelete?.name}?</p>
                <p className="text-warning">
                  <small>Hành động này sẽ không được hoàn tác.</small>
                </p>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeDeleteModal}>
                  Hủy
                </button>
                <button type="submit" className="btn btn-danger">
                  Xóa
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Success Modal */}
      <div ref={successModalRef} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title">Thông báo</h4>
              <button type="button" className="close" onClick={closeSuccessModal}>
                &times;
              </button>
            </div>
            <div className="modal-body">
              <p>Người dùng được lưu thành công!</p>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={closeSuccessModal}>
                OK
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default ManageCustomer;
