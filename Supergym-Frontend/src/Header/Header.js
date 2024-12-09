import React, { useState, useEffect, useRef } from "react";

import axios from "axios";
import { useNavigate } from "react-router-dom";
import { Helmet } from "react-helmet";
import { getRole, hasRequiredRole } from "../utils/authUtils";
import "bootstrap/dist/css/bootstrap.min.css";
import "./Header.css";

import NotificationsIcon from "@mui/icons-material/Notifications";
import MailIcon from "@mui/icons-material/Mail";
import AccountCircleIcon from "@mui/icons-material/AccountCircle";
import SettingsIcon from "@mui/icons-material/Settings";
import PowerSettingsNewIcon from "@mui/icons-material/PowerSettingsNew";
import CloseIcon from "@mui/icons-material/Close";

import logo from "../assets/images/super-gym-logo.jpg";

const Header = () => {
  const navigate = useNavigate();
  const [customerData, setCustomerData] = useState([]);
  const role = getRole();

  // MODAL
  const customerModalRef = useRef(null);
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const [errors, setErrors] = useState({});
  const [currentCustomer, setCurrentCustomer] = useState(null); // for editing customer
  const fileInputRef = useRef(null);

  useEffect(() => {
    const fetchUserData = async () => {
      try {
        const token = localStorage.getItem("token");

        if (!token) {
          console.error("Token not found");
          return;
        }

        const response = await axios.get("http://localhost:5000/api/Users/GetCurrentUser", {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        });

        setCustomerData([
          {
            userId: response.data.userId,
            name: response.data.name,
            email: response.data.email,
            gender: response.data.gender,
            dob: response.data.dob || { // Lấy dob từ response nếu có
              date: response.data.dob?.date || 0,
              month: response.data.dob?.month || 0,
              year: response.data.dob?.year || 0,
            },
            address: response.data.address,
            phone: response.data.phone,
            roleId: response.data.roleId,
            userAvatar: `data:image/png;base64,${response.data.userAvatar}`,
            idCard: response.data.idCard,
          },
        ]);
      } catch (error) {
        console.error("Error fetching user data:", error);
      }
    };

    fetchUserData();
  }, []);

  const handleLogout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    navigate("/login");
  };

  // Check if user is admin
  const isAdmin = role === "admin";

  // Function to show menu based on role
  const shouldShowMenu = (menuType) => {
    if (isAdmin) return true; // Admin sees everything

    // For staff, hide specific menus
    if (role === "staff") {
      switch (menuType) {
        case "staff-management":
        case "dashboard":
        case "feedback":
        case "trainer-management": // Added trainer management case
          return false;
        default:
          return true;
      }
    }
    return true;
  };

  // MODAL
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

  const validateField = (name, value) => {
    const newErrors = { ...errors };

    switch (name) {
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

  const handleSubmit = async (e) => {};

  const openAddCustomerModal = () => {
    if (customerData[0]) {
      const dob = customerData[0].dob || {};
      // Format dob thành dạng YYYY-MM-DD để bind vào input type="date"
      const formattedDob = `${dob.year || "----"}-${String(dob.month || "01").padStart(2, "0")}-${String(dob.date || "01").padStart(2, "0")}`;
  
      setFormData({
        name: customerData[0].name || "",
        email: customerData[0].email || "",
        phone: customerData[0].phone || "",
        address: customerData[0].address || "",
        gender: customerData[0].gender?.toLowerCase() || "male",
        dob: formattedDob,  // Bind ngày tháng năm đã format
        idCard: customerData[0].idCard || "",
        userAvatar: null
      });
  
      if (customerData[0].userAvatar) {
        const base64Image = customerData[0].userAvatar.includes('base64,')
          ? customerData[0].userAvatar.split('base64,')[1]
          : customerData[0].userAvatar.replace('data:image/png;base64,', '');
        
        setPreviewImage(base64Image);
      }
    }
  
    if (fileInputRef.current) {
      fileInputRef.current.value = null;
    }
  
    setCurrentCustomer(customerData[0]);
    customerModalRef.current.style.display = "block";
    customerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  return (
    <>
      <Helmet>
        <title>Header</title>
      </Helmet>
      <nav className="navbar navbar-expand-xl navbar-light bg-light">
        <button type="button" className="navbar-toggler" data-toggle="collapse" data-target="#navbarCollapse">
          <span className="navbar-toggler-icon"></span>
        </button>
        <div id="navbarCollapse" className="collapse navbar-collapse justify-content-start">
          <div className="navbar-nav">
            <img src={logo} alt="logo" className="logo-manage" />
          </div>

          <div className="navbar-nav">
            {/* Home - visible to all */}
            <a href="/" className="nav-item nav-link active">
              Trang chủ
            </a>

            {/* Dashboard - hidden for staff */}
            {shouldShowMenu("dashboard") && (
              <a href="/dashboard" className="nav-item nav-link active">
                Thống kê
              </a>
            )}

            {/* User Management Dropdown */}
            <div className="nav-item dropdown">
              <a href="#" className="nav-link active dropdown-toggle" id="userManagementDropdown">
                Quản lý người dùng
              </a>
              <div className="dropdown-content">
                {shouldShowMenu("staff-management") && <a href="/manageManager">Quản lý nhân sự</a>}
                <a href="/manageCustomer">Quản lý khách hàng</a>
                {shouldShowMenu("trainer-management") && (
                  <div className="dropdown-submenu">
                    <a href="#" className="dropright-toggle">
                      Quản lý huấn luyện viên
                    </a>
                    <div className="submenu-content">
                      <a href="/ManageTrainer">Thêm huấn luyện viên</a>
                      <a href="/EditTrainer">Sửa thông tin huấn luyện viên</a>
                    </div>
                  </div>
                )}
                <a href="/manageMembership">Quản lý Membership</a>
              </div>
            </div>

            {/* Equipment Management - visible to all */}
            <a href="/manageEquipment" className="nav-item nav-link active">
              Quản lý thiết bị
            </a>

            {/* Course Management - visible to all */}
            <div className="nav-item dropdown">
              <a href="" className="nav-link active dropdown-toggle" id="courseManagementDropdown">
                Quản lý gói
              </a>
              <div className="dropdown-content">
                <a href="/manageCourse">Quản lý gói tập Gym</a>
                <a href="/manageTrainerCourse">Quản lý gói Trainer</a>
                <a href="/manageBoxingCourse">Quản lý gói Boxing</a>
              </div>
            </div>

            {/* Feedback Management - hidden for staff */}
            {shouldShowMenu("feedback") && (
              <a href="/manageFeedback" className="nav-item nav-link active">
                Quản lý phản hồi
              </a>
            )}

            {/* Schedule Management - visible to all */}
            <a href="/manageSchedule" className="nav-item nav-link active">
              Quản lý lịch
            </a>
          </div>

          {/* User Profile Section */}
          <div className="navbar-nav ms-auto">
            <div className="nav-item dropdown">
              <a href="#" className="nav-link dropdown-toggle user-action" id="userDropdown">
                <img src={customerData[0]?.userAvatar || logo} alt="user-avatar" className="user-avatar" />
                <span>{customerData[0]?.name || "Guest"}</span>
                <b className="caret"></b>
              </a>

              <div className="dropdown-content">
                <a href="#" onClick={openAddCustomerModal}>
                  <AccountCircleIcon /> Thông tin cá nhân
                </a>
                {/* <a href="#">
                  <SettingsIcon /> Đổi mật khẩu
                </a> */}
                <div className="dropdown-divider"></div>
                <a href="#" onClick={handleLogout}>
                  <PowerSettingsNewIcon /> Đăng xuất
                </a>
              </div>
            </div>
          </div>
        </div>
      </nav>

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
                <label>Ảnh đại diện</label>

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

              {/* <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                  Hủy
                </button>

                <button type="submit" className="btn btn-success">
                  Lưu
                </button>
              </div> */}
            </form>
          </div>
        </div>
      </div>
      {/* Modal Overlay */}
      <div className="modal-overlay"></div>
    </>
  );
};

export default Header;
