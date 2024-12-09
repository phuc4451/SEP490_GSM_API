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
  const [searchQuery, setSearchQuery] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  //PRELOAD
  const [customerData, setCustomerData] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [isTimeoutFinished, setIsTimeoutFinished] = useState(false);
  //END PRELOAD

  const [currentCustomer, setCurrentCustomer] = useState(null); // for editing customer
  const [customerToDelete, setCustomerToDelete] = useState(null); // for deletion
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const fileInputRef = useRef(null);
  const [successMessage, setSuccessMessage] = useState("");


  const [formData, setFormData] = useState({
    name: "",
    gender: "male",
    dob: "",
    email: "",
    phone: "",
    // userEnabled: "active",
    // role: "staff",
    userAvatar: null,
    idCard: "",
  });
  const [errors, setErrors] = useState({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8; // Maximum 2 employees per page

  const customerModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);
  const errorModalRef = useRef(null);

  //FETCH DATA AND PRELOAD
  useEffect(() => {
    const fetchCustomers = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Users/GetCustomerAccounts", {
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
      gender: customer.gender,
      dob: formattedDob,
      email: customer.email,
      phone: customer.phone,
      address: customer.address,
      userId: customer.userId,
      idCard: customer.idCard,
      userAvatar: customer.userAvatar,
    });
    if (customer.userAvatar) {
      setPreviewImage(customer.userAvatar); // Set the current avatar to preview image
    } else {
      setPreviewImage(null); // No avatar, clear the preview
    }
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
    const { name, value } = e.target;
  
    setFormData({
      ...formData,
      [name]: value,
    });
  
    // Trigger validation for the specific field
    validateField(name, value);
  };
  
  const validateField = (name, value) => {
    const newErrors = { ...errors };
  
    switch (name) {
      case "name":
        if (value.length < 5 || value.length > 30) {
          newErrors.name = "Họ tên phải từ 5 đến 30 ký tự.";
        } else {
          delete newErrors.name; // Clear error if valid
        }
        break;
      case "dob":
        const dob = new Date(value);
        const currentDate = new Date();
        if (dob >= currentDate) {
          newErrors.dob = "Ngày tháng năm sinh phải nhỏ hơn ngày hiện tại.";
        } else {
          delete newErrors.dob; // Clear error if valid
        }
        break;
      case "email":
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(value)) {
          newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
        } else {
          delete newErrors.email; // Clear error if valid
        }
        break;
      case "phone":
        if (value.length < 9 || value.length > 11 || !/^\d+$/.test(value)) {
          newErrors.phone = "Số điện thoại phải từ 9 đến 11 chữ số.";
        } else {
          delete newErrors.phone; // Clear error if valid
        }
        break;
      case "address":
        if (!value) {
          newErrors.address = "Địa chỉ không được để trống.";
        } else {
          delete newErrors.address; // Clear error if valid
        }
        break;
      case "idCard":
        if (!value) {
          newErrors.idCard = "Số căn cước không được để trống.";
        } else {
          delete newErrors.idCard; // Clear error if valid
        }
        break;
      case "gender":
        if (!value) {
          newErrors.gender = "Vui lòng chọn giới tính.";
        } else {
          delete newErrors.gender; // Clear error if valid
        }
        break;
      default:
        break;
    }
  
    setErrors(newErrors);
  };

  const validateForm = () => {
    const newErrors = {};
  
    // Check all fields here
    if (formData.name.length < 5 || formData.name.length > 30) {
      newErrors.name = "Họ tên phải từ 5 đến 30 ký tự.";
    }
  
    const dob = new Date(formData.dob);
    const currentDate = new Date();
    if (dob >= currentDate) {
      newErrors.dob = "Ngày tháng năm sinh phải nhỏ hơn ngày hiện tại.";
    }
  
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailPattern.test(formData.email)) {
      newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
    }
  
    if (formData.phone.length < 9 || formData.phone.length > 11 || !/^\d+$/.test(formData.phone)) {
      newErrors.phone = "Số điện thoại phải từ 9 đến 11 chữ số.";
    }
  
    if (!formData.address) {
      newErrors.address = "Địa chỉ không được để trống.";
    }
  
    if (!formData.idCard) {
      newErrors.idCard = "Số căn cước không được để trống.";
    }
  
    if (!formData.gender) {
      newErrors.gender = "Vui lòng chọn giới tính.";
    }
  
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // If no errors, form is valid
  };


  const showSuccessModal = (message) => {
    setSuccessMessage(message); // Cập nhật thông báo
    successModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Hàm để đóng modal thông báo
  const closeSuccessModal = () => {
    successModalRef.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    window.location.reload();
  };

  // Hàm để mở modal thông báo
  const showErrorModal = (message) => {
    setErrorMessage(message); // Cập nhật thông báo
    errorModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Hàm để đóng modal thông báo
  const closeErrorModal = () => {
    errorModalRef.current.style.display = "none"; // Ẩn modal
    // document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    // window.location.reload();
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
  
      try {
        if (currentCustomer) {
          const customerDataEdit = {
            userId: currentCustomer.userId,
            name: formData.name,
            email: formData.email,
            gender: formData.gender,
            dob: dobData,
            address: formData.address,
            phone: formData.phone,
            roleId: "string",
            userAvatar: formData.userAvatar,
            idCard: formData.idCard,
          };
      
          // Update customer
          await axios.patch(`http://localhost:5000/api/Users/updateCustomer/${currentCustomer.userId}`, customerDataEdit, {
            headers: { Authorization: `Bearer ${token}` },
          });
  
          // Update customer data in the list
          setCustomerDataList((prevData) => prevData.map((customer) => (customer.userId === currentCustomer.userId ? { ...customer, ...customerData } : customer)));
        } else {
          const customerDataAdd = {
            email: formData.email,
            name: formData.name,
            gender: formData.gender,
            dob: dobData,
            address: formData.address,
            phone: formData.phone,
            userAvatar: formData.userAvatar,
            idCard: formData.idCard,
          };
          // Add new customer
          const response = await axios.post("http://localhost:5000/api/Users/addcustomer", customerDataAdd, {
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
        closeModal();
        showSuccessModal("Người dùng được lưu thành công");
      } catch (error) {
        console.error("Error saving customer:", error);
        showErrorModal(error.message);

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

        setCustomerData(customerData.filter((cus) => cus.id !== customerToDelete.id));
        closeModal();
        closeDeleteModal();
        showSuccessModal("Xóa người dùng thành công!");
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
      // Use FileReader to convert image to base64
      const reader = new FileReader();
  
      reader.onloadend = () => {
        // Extract base64 data without the "data:image/jpeg;base64," prefix
        const base64String = reader.result.split(',')[1]; // Remove the prefix
  
        // Set preview image for display (include the full base64 string with prefix)
        setPreviewImage(reader.result.split(',')[1]); // Set only the image data for preview
  
        // Store base64 image without the prefix in formData
        setFormData({
          ...formData,
          userAvatar: base64String, // Store base64 image without the prefix in formData
        });
      };
  
      reader.readAsDataURL(file); // Convert the file to base64
    }
  };

  const handleSearchChange = (e) => {
    setSearchQuery(e.target.value); // Cập nhật nội dung tìm kiếm khi người dùng nhập
  };
  const filteredCustomerData = customerData.filter(
    (Customer) => Customer.email.toLowerCase().includes(searchQuery.toLowerCase()) // Tìm kiếm theo email
  );

  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageCustomer */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}

      <div className="user-select">
        <h1>Quản lý khách hàng trong hệ thống super gym</h1>

        <div className="select-search-container">
          <div className="search-container">
            <input
              type="text"
              id="searchUser"
              className="form-control"
              placeholder="Tìm kiếm theo email..."
              value={searchQuery} // Liên kết với state searchQuery
              onChange={handleSearchChange} // Cập nhật state khi người dùng nhập
            />
            <span className="search-icon">
              <SearchIcon />
            </span>
          </div>
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
                <th className="action-el">Hành động</th>
              </tr>
            </thead>
            <tbody>
            {filteredCustomerData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((customer, index) => (
                <tr key={index}>
                  <td>
                    <img src={`data:image/jpeg;base64,${customer.userAvatar}`} className="customer-avatar" />
                    {customer.name}
                  </td>
                  {/* <td>{customer.gender}</td> */}
                  <td>{customer.gender === "male" ? "Nam" : "Nữ"}</td>
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
                <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Sửa thông tin khách hàng" : "Thêm khách hàng"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Họ tên <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.name ? "is-invalid" : ""}`} name="name" value={formData.name} onChange={handleInputChange} required />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Ngày tháng năm sinh <span className="icon-input">(*)</span>
                    </label>
                    <input type="date" className={`form-control ${errors.dob ? "is-invalid" : ""}`} name="dob" value={formData.dob} onChange={handleInputChange} required />
                    {errors.dob && <div className="error-message">{errors.dob}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Email <span className="icon-input">(*)</span>
                    </label>
                    <input type="email" className={`form-control ${errors.email ? "is-invalid" : ""}`} name="email" value={formData.email} onChange={handleInputChange} required />
                    {errors.email && <div className="error-message">{errors.email}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Số điện thoại <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.phone ? "is-invalid" : ""}`} name="phone" value={formData.phone} onChange={handleInputChange} required />
                    {errors.phone && <div className="error-message">{errors.phone}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Ảnh đại diện</label>
                    <input type="file" className="form-control" onChange={handleImageChange} accept="image/*" ref={fileInputRef} />
                  </div>

                  {previewImage && (
                    <div className="form-group col">
                      <img src={`data:image/jpeg;base64,${previewImage}`} alt="Ảnh xem trước" className="preview-image" />
                    </div>
                  )}
                </div>


                <div className="row">
                  <div className="form-group col">
                    <label>
                      Địa chỉ <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.address ? "is-invalid" : ""}`} name="address" value={formData.address} onChange={handleInputChange} required />
                    {errors.address && <div className="error-message">{errors.address}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Số căn cước <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.idCard ? "is-invalid" : ""}`} name="idCard" value={formData.idCard} onChange={handleInputChange} required />
                    {errors.idCard && <div className="error-message">{errors.idCard}</div>}
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
                <button type="button" className="btn btn-default" onClick={closeModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                  Hủy
                </button>

                <button type="submit" className="btn btn-success" >
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
                <h4 className="modal-title text-center mx-auto">Xóa người dùng</h4>
                <a type="button" className="close" onClick={closeDeleteModal}>
                <CloseIcon />
                </a>
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
              <h4 className="modal-title text-center mx-auto">Thông báo</h4>
              <a type="button" className="close" onClick={closeSuccessModal}>
              <CloseIcon />

              </a>
            </div>
            <div className="modal-body">
            <p>{successMessage}</p>

            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={closeSuccessModal}>
                OK
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Error Modal */}
      <div ref={errorModalRef} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Thông báo</h4>
              <a type="button" className="close" onClick={closeErrorModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              <p>{errorMessage}</p> {/* Hiển thị lỗi từ state */}
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={closeErrorModal}>
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
