import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./ManageManager.css";

// require('dotenv').config()
import MonetizationOnIcon from '@mui/icons-material/MonetizationOn';
import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import { Delete } from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { getRole, Logout } from "../utils/authUtils";
import LoadingSpinner from "../utils/LoadingOverlay";

const ManageCustomer = () => {
  const navigate = useNavigate();
  useEffect(() => {
    const userRole = getRole();
    if (userRole !== "admin") {
      Logout();
      navigate("/login"); // or redirect to login
      return;
    }
  }, [navigate]);
  const [StaffDataList, setStaffDataList] = useState([]);
  const fileInputRef = useRef(null);
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const [searchQuery, setSearchQuery] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

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
  const itemsPerPage = 8; // Maximum 2 employees per page

  const StaffModalRef = useRef(null);
  const successModalRef = useRef(null);
  const errorModalRef = useRef(null);
  const deleteModalRef = useRef(null);
  const [isSaving, setIsSaving] = useState(false);
  const salaryModalRef = useRef(null);

  //FETCH DATA AND PRELOAD
  useEffect(() => {
    const fetchStaffs = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Users/getStaffAccounts", {
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
      address: "",
      idCard: "",
      userAvatar: "",
    });
    setCurrentStaff(null);
    setPreviewImage(null); // Reset preview image
    if (fileInputRef.current) {
      fileInputRef.current.value = ""; // Reset file input
    }
    setErrors({}); 
    StaffModalRef.current.style.display = "block";
    StaffModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };
  const openEditStaffModal = (Staff) => {
    // Convert DOB object to a date string compatible with the input field
    const formattedDob = `${Staff.dob.year}-${String(Staff.dob.month).padStart(2, "0")}-${String(Staff.dob.date).padStart(2, "0")}`;

    // Set form data
    setFormData({
      name: Staff.name,
      gender: Staff.gender,
      dob: formattedDob, // Set formatted date
      email: Staff.email,
      phone: Staff.phone,
      address: Staff.address,
      idCard: Staff.idCard,
      userAvatar: Staff.userAvatar,
    });

    // Set preview image if there is an avatar
    if (Staff.userAvatar) {
      setPreviewImage(Staff.userAvatar); // Set the current avatar to preview image
    } else {
      setPreviewImage(null); // No avatar, clear the preview
    }

    setCurrentStaff(Staff); // Set to editing mode by assigning the Staff
    StaffModalRef.current.style.display = "block";
    StaffModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeModal = () => {
    salaryModalRef.current.style.display = "none";
    salaryModalRef.current.classList.remove("active");
    StaffModalRef.current.style.display = "none";
    StaffModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentStaff(null); // Reset current Staff to null when closing modal
    
    // Thêm các dòng này để reset ảnh
    setPreviewImage(null); // Reset preview image
    if (fileInputRef.current) {
      fileInputRef.current.value = ""; // Reset file input
    }
    setErrors({}); 
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
          // Kiểm tra nếu không phải là số hoặc không đúng 12 chữ số
          if (!value || !/^\d+$/.test(value) || value.length !== 12) {
            newErrors.idCard = "Số căn cước phải là số và có 12 chữ số.";
          } else {
            delete newErrors.idCard;
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

  // Hàm để mở modal thông báo
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

  const openDeleteModal = (Staff) => {
    setStaffToDelete(Staff); // Set Staff to be deleted
    deleteModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeDeleteModal = () => {
    deleteModalRef.current.style.display = "none"; // Hide delete modal
    document.querySelector(".modal-overlay").style.display = "none"; // Hide overlay
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

    if (!formData.idCard || !/^\d+$/.test(formData.idCard) || formData.idCard.length !== 12) {
      newErrors.idCard = "Số căn cước phải là số và có đúng 12 chữ số.";
    }

    if (!formData.gender) {
      newErrors.gender = "Vui lòng chọn giới tính.";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // If no errors, form is valid
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (validateForm()) {
      const token = localStorage.getItem("token");
      setIsSaving(true); 

      // Format date to the required format
      const dob = new Date(formData.dob);
      const dobData = {
        date: dob.getDate(),
        month: dob.getMonth() + 1,
        year: dob.getFullYear(),
      };

      // Create a basic Staff data object
      const StaffData = {
        email: formData.email,
        name: formData.name,
        gender: formData.gender,
        dob: dobData,
        address: formData.address,
        phone: formData.phone,
        userAvatar: formData.userAvatar,
        idCard: formData.idCard,
        position: "string",
      };

      try {
        if (currentStaff) {
          const StaffDataEdit = {
            userId: currentStaff.userId,
            email: formData.email,
            name: formData.name,
            gender: formData.gender,
            dob: dobData,
            address: formData.address,
            phone: formData.phone,
            userAvatar: formData.userAvatar,
            idCard: formData.idCard,
            roleId: "string",
          };
          // Update Staff
          await axios.patch(`http://localhost:5000/api/Users/updateStaff/${currentStaff.userId}`, StaffDataEdit, {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          });

          // Update Staff data in the list
          setStaffDataList((prevData) => prevData.map((Staff) => (Staff.userId === currentStaff.userId ? { ...Staff, ...StaffData } : Staff)));
        } else {
          // Add new Staff
          console.log(StaffData);
          const response = await axios.post("http://localhost:5000/api/Users/addStaff", StaffData, {
            headers: {
              Authorization: `Bearer ${token}`,
            },
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
        closeModal();
        showSuccessModal("Người dùng được lưu thành công");
      } catch (error) {
        console.error("Error saving Staff:", error);
        showErrorModal(error.message);
      } finally {
        setIsSaving(false); // Stop loading
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

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      // Use FileReader to convert image to base64
      const reader = new FileReader();

      reader.onloadend = () => {
        // Extract base64 data without the "data:image/jpeg;base64," prefix
        const base64String = reader.result.split(",")[1]; // Remove the prefix

        // Set preview image for display (include the full base64 string with prefix)
        setPreviewImage(reader.result.split(",")[1]); // Set only the image data for preview

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
  const filteredStaffData = StaffData.filter(
    (Staff) => Staff.email.toLowerCase().includes(searchQuery.toLowerCase()) // Tìm kiếm theo email
  );

  // const openViewSalaryModal = async (feedback) => {
    const openViewSalaryModal = () => {
    // setIsLoadingUser(true);
    // const token = localStorage.getItem("token");

    // try {
    //   const userResponse = await axios.get(
    //     `http://localhost:5000/api/Users/GetUserById/${feedback.userId}`,
    //     { headers: { Authorization: `Bearer ${token}` } }
    //   );
    //   setCurrentUserData(userResponse.data);
    //   setCurrentFeedback(feedback);
    // } catch (error) {
    //   console.error(`Error fetching user data:`, error);
    //   setCurrentUserData({ name: "Unknown", email: "Unknown" });
    // } finally {
      // setIsLoadingUser(false);
      salaryModalRef.current.style.display = "block";
      salaryModalRef.current.classList.add("active");
      document.querySelector(".modal-overlay").style.display = "block";
    // }
  };
  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageCustomer */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}
      {isSaving && <LoadingSpinner isLoading={true} />}

      <div className="user-select">
        <h1>Quản lí nhân viên trong hệ thống SUPER GYM</h1>

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
              {filteredStaffData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((Staff, index) => (
                <tr key={index}>
                  <td>
                    <img src={`data:image/jpeg;base64,${Staff.userAvatar}`} className="Staff-avatar" />
                    {Staff.name}
                  </td>
                  <td>{Staff.gender === "male" ? "Nam" : "Nữ"}</td>
                  <td>{formatDate(Staff.dob)}</td>
                  <td>{Staff.email}</td>
                  <td>{Staff.phone}</td>
                  <td>{Staff.address}</td>
                  <td>
                    <a href="#" onClick={() => openEditStaffModal(Staff)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openViewSalaryModal(Staff)} className="view">
                      <MonetizationOnIcon />
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
                <h4 className="modal-title text-center mx-auto">{currentStaff ? "Sửa thông tin nhân viên" : "Thêm nhân viên"}</h4>
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
                    <input type="email" className={`form-control ${errors.email ? "is-invalid" : ""}`} name="email" value={formData.email} onChange={handleInputChange} required disabled={currentStaff !== null} />
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
                    <label>
                      Giới tính <span className="icon-input">(*)</span>
                    </label>
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
                    {errors.gender && <div className="error-message">{errors.gender}</div>}
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} disabled={isSaving}>
                  Hủy
                </button>
                <button type="submit" className="btn btn-success" disabled={isSaving}>
                  Lưu
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      <div ref={salaryModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Lương nhân viên</h4>
              <a type="button" className="close" onClick={closeModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              {/* {isLoadingUser ? (
                <div className="text-center">
                  <div className="spinner-border" role="status">
                    <span className="visually-hidden">Loading...</span>
                  </div>
                </div>
              ) : (
                currentFeedback &&
                currentUserData && (
                  <> */}
                    <p>
                      <strong>Email:</strong> 
                    </p>
                    <p>
                      <strong>Tên:</strong> 
                    </p>
                    <p>
                      <strong>Số ngày checkin trong tháng:</strong>
                    </p>
                    <p>
                      <strong>Lương cơ bản:</strong>
                    </p>
                    <p>
                      <strong>Số ngày đã checkin trong tháng:</strong>
                    </p>
                    <p>
                      <strong>Lương được nhận:</strong> 
                    </p>
                  {/* </>
                )
              )} */}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={closeModal}>
                Đóng
              </button>
            </div>
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
              <h4 className="modal-title text-center mx-auto">Thông báo</h4>
              <a type="button" className="close" onClick={closeSuccessModal}>
                <CloseIcon />
              </a>
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
