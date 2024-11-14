import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./ManageCustomer.css";

import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import { Delete } from "@mui/icons-material";

const ManageCustomer = () => {
  const [customerDataList, setCustomerDataList] = useState([]);
  //PRELOAD
  const [customerData, setCustomerData] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [isTimeoutFinished, setIsTimeoutFinished] = useState(false);
  //END PRELOAD

  const [currentCustomer, setCurrentCustomer] = useState(null); // for editing customer
  const [customerToDelete, setCustomerToDelete] = useState(null); // for deletion
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const fileInputRef = useRef(null);

  const [formData, setFormData] = useState({
    name: "",
    gender: "male",
    dob: "",
    email: "",
    phone: "",
    // userEnabled: "active",
    // role: "staff",
    userAvatar: null,
  });
  const [errors, setErrors] = useState({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 4; // Maximum 2 employees per page

  const customerModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);

  //FETCH DATA AND PRELOAD
  useEffect(() => {
    const fetchCustomers = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/users/getCustomers", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setCustomerData(response.data);
      } catch (error) {
        console.error("Error fetching customers:", error);
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchCustomers();
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
  const totalPages = Math.ceil(customerData.length / itemsPerPage);
  const currentCustomers = customerData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);
  const formatDate = (dob) => {
    const date = dob?.date ?? "--";
    const month = dob?.month ?? "--";
    const year = dob?.year ?? "----";
    return `${date}/${month}/${year}`;
  };
  const handlePageChange = (pageNumber) => {
    setCurrentPage(pageNumber);
  };
  //END PAGENATION

  const openAddCustomerModal = () => {
    setFormData({
      name: "",
      gender: "male",
      dob: "",
      email: "",
      phone: "",
      address: "",
      userAvatar: null, // Đặt lại ảnh đã chọn
    });
    setPreviewImage(null); // Đặt lại ảnh xem trước
    if (fileInputRef.current) {
      fileInputRef.current.value = null; // Đặt lại giá trị input file
    }
    setCurrentCustomer(null); // Đặt lại khách hàng hiện tại để thêm mới
    customerModalRef.current.style.display = "block"; // Hiển thị modal
    customerModalRef.current.classList.add("active"); // Thêm class 'active'
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openEditCustomerModal = (customer) => {
    const dob = customer.dob || {};
    const formattedDob = `${dob.year || "----"}-${String(dob.month || "01").padStart(2, "0")}-${String(dob.date || "01").padStart(2, "0")}`;

    setFormData({
      name: customer.name,
      gender: customer.gender.toLowerCase() === "nam" ? "male" : "female",
      dob: formattedDob,
      email: customer.email,
      phone: customer.phone,
      address: customer.address,
    });
    setCurrentCustomer(customer);
    customerModalRef.current.style.display = "block";
    customerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeModal = () => {
    customerModalRef.current.style.display = "none";
    customerModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentCustomer(null); // Đặt lại khách hàng hiện tại
    setPreviewImage(null); // Đặt lại ảnh xem trước
    setFormData({
      name: "",
      gender: "male",
      dob: "",
      email: "",
      phone: "",
      address: "",
      userAvatar: null, // Đặt lại ảnh đã chọn
    });
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

  const openDeleteModal = (customer) => {
    setCustomerToDelete(customer); // Set customer to be deleted
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

      // Create a basic customer data object
      const customerData = {
        userId: currentCustomer ? currentCustomer.userId : "string",
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

      // Include password only for adding a new customer
      if (!currentCustomer) {
        customerData.password = formData.password;
      }

      try {
        if (currentCustomer) {
          // Update customer
          await axios.put(`http://localhost:5000/api/Users/updatecustomer/${currentCustomer.userId}`, customerData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          // Update customer data in the list
          setCustomerDataList((prevData) => prevData.map((customer) => (customer.userId === currentCustomer.userId ? { ...customer, ...customerData } : customer)));
        } else {
          // Add new customer
          const response = await axios.post("http://localhost:5000/api/Users/addcustomer", customerData, {
            headers: { Authorization: `Bearer ${token}` },
          });

          setCustomerDataList([...customerDataList, response.data]);
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
        console.error("Error saving customer:", error);
      }
    }
  };

  const deleteCustomer = async () => {
    if (customerToDelete) {
      const token = localStorage.getItem("token");
      try {
        await axios.delete(`http://localhost:5000/api/users/${customerToDelete.userId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        // Xóa khách hàng từ danh sách
        setCustomerData(customerData.filter((cus) => cus.id !== customerToDelete.id));

        // Ẩn modal và reset khách hàng cần xóa
        closeDeleteModal();
      } catch (error) {
        console.error("Error deleting customer:", error);
      }
    }
  };
  const handleDeleteEmployee = (e) => {
    e.preventDefault();
    deleteCustomer();
  };

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      setFormData({
        ...formData,
        userAvatar: file,
      });

      const reader = new FileReader();
      reader.onloadend = () => {
        setPreviewImage(reader.result);
      };
      reader.readAsDataURL(file);
    }
  };

  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageCustomer */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}

      <div className="user-select">
        <h1>Quản lý người dùng trong hệ thống super gym</h1>

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
                <h2>Quản lí khách hàng</h2>
              </div>
              <div className="col-sm-6">
                <button onClick={openAddCustomerModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm mới khách hàng</span>
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
              {currentCustomers.map((customer, index) => (
                <tr key={index}>
                  <td>
                    <img src={`data:image/jpeg;base64,${customer.userAvatar}`} className="customer-avatar" />
                    {customer.name}
                  </td>
                  {/* <td>{customer.gender}</td> */}
                  <td>{customer.gender === "Male" ? "Nam" : "Nữ"}</td>
                  <td>{formatDate(customer.dob)}</td>
                  <td>{customer.email}</td>
                  <td>{customer.phone}</td>
                  <td>{customer.address}</td>
                  {/* <td className={customer.status === "Hoạt động" ? "status-el-active" : "status-el-inactive"}>{customer.status}</td> */}
                  {/* <td>{customer.role}</td> */}
                  <td>
                    <a href="#" onClick={() => openEditCustomerModal(customer)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openDeleteModal(customer)} className="delete">
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
              Showing <b>{currentCustomers.length}</b> out of <b>{customerData.length}</b> entries
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

      {/* customer Modal */}
      <div ref={customerModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="employeeForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Sửa thông tin người dùng" : "Thêm khách hàng"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>Họ tên <span className="icon-input">(*)</span></label>
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
                    <label>Số điện thoại <span className="icon-input">(*)</span></label>
                    <input type="text" className="form-control" name="phone" value={formData.phone} onChange={handleInputChange} required />
                    {errors.phone && <div className="error-message">{errors.phone}</div>}
                  </div>
                </div>

                {/* Conditionally render the password input only if adding a new customer */}
                {!currentCustomer && (
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
                    <input type="text" className="form-control " name="address" value={formData.address} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group col">
                    <label>Chọn gói</label>
                    <select
                      className="form-control form-select"
                      name="package"
                      value={formData.package}
                      onChange={handleInputChange}
                    >
                      <option value="Gói tập 1 tháng">Gói tập 1 tháng</option>
                      <option value="Gói tập 3 tháng">Gói tập 3 tháng</option>
                    </select>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Ảnh nhận diện <span className="icon-input">(*)</span></label>
                    <input
                      type="file"
                      className="form-control"
                      onChange={handleImageChange}
                      accept="image/*"
                      ref={fileInputRef} // Thêm ref ở đây
                    />
                  </div>
                  {previewImage && (
                    <div className="form-group col">
                      <img src={previewImage} alt="Ảnh xem trước" className="preview-image" />
                    </div>
                  )}
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
            <form id="deleteEmployeeForm" onSubmit={handleDeleteEmployee}>
              <div className="modal-header">
                <h4 className="modal-title">Xóa người dùng</h4>
                <button type="button" className="close" onClick={closeDeleteModal}>
                  &times;
                </button>
              </div>
              <div className="modal-body">
                <p>Bạn có chắc chắn muốn xóa {customerToDelete?.name}?</p>
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
