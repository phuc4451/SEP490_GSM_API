import React, { useState, useEffect, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "../assets/css/common.css";
import "./ManageMembership.css";

import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { Delete } from "@mui/icons-material";
import LoadingSpinner from "../utils/LoadingOverlay";

const ManageMembership = () => {
  const [membershipDataList, setMembershipDataList] = useState([]); // renamed variable
  const [selectedOption, setSelectedOption] = useState(""); // State cho gói đã chọn
  const [selectedPackages, setSelectedPackages] = useState([]); // Dữ liệu các gói
  const [qrDataUrl, setQrDataUrl] = useState(""); // State to store the QR code data URL
  const showQrPicture = useRef(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  //PRELOAD
  const [membershipData, setMembershipData] = useState([]); // renamed variable
  const [packageData, setpackageData] = useState([]); // renamed variable
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [isTimeoutFinished, setIsTimeoutFinished] = useState(false);
  //END PRELOAD

  const [currentMembership, setCurrentMembership] = useState(null); // renamed variable
  const [membershipToDelete, setMembershipToDelete] = useState(null); // renamed variable
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const fileInputRef = useRef(null);
  const [successMessage, setSuccessMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const detailModalRef = useRef(null);
  const [isLoadingUser, setIsLoadingUser] = useState(false);
  const [arePackagesLoaded, setArePackagesLoaded] = useState(false);

  const [membershipDetails, setMembershipDetails] = useState({
    gymMemberships: [],
    boxingOptions: [],
    rentalOptions: [],
  });

  const [formData, setFormData] = useState({
    name: "",
    gender: "male",
    dob: "",
    email: "",
    phone: "",
    qrPayment: "true",
    userAvatar: null,
  });
  const [errors, setErrors] = useState({});
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 8; // Maximum 2 employees per page

  const membershipModalRef = useRef(null); // renamed ref
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);
  const errorModalRef = useRef(null);

  //FETCH DATA AND PRELOAD
  useEffect(() => {
    const fetchMemberships = async () => {
      // renamed function
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Users/GetAllMemberships", {
          // updated API endpoint
          headers: { Authorization: `Bearer ${token}` },
        });
        setMembershipData(response.data); // updated variable
      } catch (error) {
        console.error("Error fetching memberships:", error); // renamed error message
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchMemberships(); // renamed function call
  }, []);

  useEffect(() => {
    const fetchPackage = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/GymMembership", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setSelectedPackages(response.data);
        setArePackagesLoaded(true); // Indicate packages are loaded
      } catch (error) {
        console.error("Error fetching memberships:", error);
        showErrorModal("Không thể tải danh sách gói tập. Vui lòng thử lại sau.");
      }
    };
    fetchPackage();
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setIsTimeoutFinished(true); // Timeout finished
    }, 500);

    return () => clearTimeout(timer); // Clear timeout on unmount
  }, []);

  const isLoading = isDataLoading || !isTimeoutFinished;

  //PAGENATION
  const totalPages = Math.ceil(membershipData.length / itemsPerPage); // renamed variable
  const currentMemberships = membershipData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage); // renamed variable
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

  const handlePackageChange = (e) => {
    const selectedPackageId = e.target.value;
    setSelectedOption(selectedPackageId); // Update selected package state
    setFormData({ ...formData, gymMembershipId: selectedPackageId });
  };

  const openAddMembershipModal = (membership) => {
    // renamed function
    // setFormData({
    //   name: "",
    //   gender: "male",
    //   dob: "",
    //   email: "",
    //   phone: "",
    //   userAvatar: null, // Đặt lại ảnh đã chọn

    //   email: membership.user.email,

    // });

    setCurrentMembership(null); // renamed variable
    membershipModalRef.current.style.display = "block"; // renamed modal ref
    membershipModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openEditMembershipModal = (membership) => {
    // renamed function
    const dob = membership.user.dob || {}; // renamed variable
    const formattedDob = `${dob.year || "----"}-${String(dob.month || "01").padStart(2, "0")}-${String(dob.date || "01").padStart(2, "0")}`;

    setFormData({
      name: membership.user.name,
      gender: membership.user.gender,
      dob: formattedDob,
      email: membership.user.email,
      phone: membership.user.phone,
      address: membership.user.address,
      userId: membership.user.userId,
    });
    setCurrentMembership(membership); // renamed variable
    membershipModalRef.current.style.display = "block"; // renamed modal ref
    membershipModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openDetailModal = async (membership) => {
    setIsLoadingUser(true); // Bật loading
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay ngay từ đầu

    const token = localStorage.getItem("token");

    try {
      const response = await axios.get(`http://localhost:5000/api/Users/membershipDetails/${membership.user.userId}`, { headers: { Authorization: `Bearer ${token}` } });
      setMembershipDetails(response.data);
      detailModalRef.current.style.display = "block";
      detailModalRef.current.classList.add("active");
    } catch (error) {
      console.error(`Error fetching membership details:`, error);
      showErrorModal("Có lỗi khi tải thông tin chi tiết");
      // Trong trường hợp lỗi, vẫn giữ overlay và hiển thị form
      detailModalRef.current.style.display = "block";
      detailModalRef.current.classList.add("active");
    } finally {
      setIsLoadingUser(false); // Tắt loading
    }
  };

  const closeDetailModal = () => {
    detailModalRef.current.style.display = "none";
    detailModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setMembershipDetails({
      gymMemberships: [],
      boxingOptions: [],
      rentalOptions: [],
    });
  };

  const closeModal = () => {
    membershipModalRef.current.style.display = "none"; // renamed modal ref
    membershipModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none"; // Hiển thị overlay
    setCurrentMembership(null); // renamed variable
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
      case "email":
        const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailPattern.test(value)) {
          newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";
        } else {
          delete newErrors.email; // Clear error if valid
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

    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailPattern.test(formData.email)) {
      newErrors.email = "Vui lòng nhập địa chỉ email hợp lệ.";

      setErrors(newErrors);
      return Object.keys(newErrors).length === 0; // If no errors, form is valid
    }
  };

  const showSuccessModal = (message) => {
    setSuccessMessage(message); // Cập nhật thông báo
    successModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const closeSuccessModal = () => {
    successModalRef.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    window.location.reload();
  };
  const showQr = () => {
    showQrPicture.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Hàm để đóng modal thông báo
  const closeQr = () => {
    showQrPicture.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    window.close();
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
    errorModalRef.current.style.display = "none";
    // Chỉ ẩn overlay nếu modal detail không được hiển thị
    // if (detailModalRef.current.style.display !== "block") {
    //   document.querySelector(".modal-overlay").style.display = "none";
    // }
  };

  const openDeleteModal = (membership) => {
    // renamed function
    setMembershipToDelete(membership); // renamed variable
    deleteModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeDeleteModal = () => {
    deleteModalRef.current.style.display = "none"; // Hide delete modal
    document.querySelector(".modal-overlay").style.display = "none"; // Hide overlay
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
  
    // Collect form data Gym
    const emailInputs = Array.from(document.querySelectorAll('input[type="email"]')).map((input) => input.value);
    const packageInput = formData.gymMembershipId;
    const qrPaymentInput = formData.qrPayment === "true";
  
    // Prepare data for Gym
    const gymData = {
      emails: [formData.email],
      boxingMembershipPlanId: null,
      gymMembershipId: packageInput,
      trainerRentalPlanId: null,
      qrPayment: qrPaymentInput,
      duration: 0,
      selectedTimeSlot: "",
      isMonWedFri: true,
    };
  
    setIsSubmitting(true);
  
    try {
      const token = localStorage.getItem("token");
  
      const response = await fetch("http://localhost:5000/api/GymRegistration", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(gymData),
      });
  
      let responsejson = null;
      const contentType = response.headers.get("content-type");
      if (contentType && contentType.includes("application/json")) {
        try {
          responsejson = await response.json();
        } catch (error) {
          console.error("Error parsing JSON:", error);
        }
      }
  
      // Check if the response indicates success
      if (response.ok) {
        closeModal();
  
        // Nếu là thanh toán QR và có QR URL
        if (qrPaymentInput && responsejson && responsejson[0] && responsejson[0].qrDataUrl) {
          setQrDataUrl(responsejson[0].qrDataUrl);
          showQr();
        }
  
        if (!qrPaymentInput && responsejson) {
          const money = responsejson;
          console.log("Money to pay:", money);
  
          if (money !== undefined && money !== null) {
            const formattedMoney = new Intl.NumberFormat('vi-VN', {
              style: 'currency',
              currency: 'VND'
            }).format(money);
            
            showSuccessModal(`Đăng ký thành công! Số tiền cần thanh toán: ${formattedMoney}`);
          } else {
            // Fallback message nếu không có moneyToPay
            showSuccessModal("Đăng ký huấn luyện viên thành công");
            console.log("No money value found in response");
          }
        }
      } else {
        // If response isn't ok, show error message from server if available
        const errorMessage = responsejson?.message || "Có lỗi khi đăng ký thành viên. Vui lòng thử lại.";
        showErrorModal(errorMessage);
      }
    } catch (error) {
      console.error("Error during membership registration:", error);
      showErrorModal("Có lỗi khi đăng ký thành viên. Vui lòng thử lại.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const deleteMembership = async () => {
    // renamed function
    if (membershipToDelete) {
      const token = localStorage.getItem("token");
      try {
        await axios.delete(`http://localhost:5000/api/users/${membershipToDelete.userId}`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        setMembershipData(membershipData.filter((membership) => membership.user.id !== membershipToDelete.id)); // renamed variable
        closeModal();
        closeDeleteModal();
        showSuccessModal("Xóa người dùng thành công!"); // renamed success message
      } catch (error) {
        console.error("Error deleting membership:", error); // renamed error message
      }
    }
  };

  const handleDeleteMembership = (e) => {
    e.preventDefault();
    deleteMembership(); // renamed function
  };
  const handleSearchChange = (e) => {
    setSearchQuery(e.target.value); // Cập nhật nội dung tìm kiếm khi người dùng nhập
  };
  const filteredMembershipData = membershipData.filter(
    (Membership) => Membership.user.email.toLowerCase().includes(searchQuery.toLowerCase()) // Tìm kiếm theo email
  );
  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageMembership */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}
      {isSubmitting && <LoadingSpinner isLoading={true} />}
      {isLoadingUser && <LoadingSpinner isLoading={true} />}
      <div className="user-select">
        <h1>Quản lý người dùng đăng kí gói trong hệ thống super gym</h1>

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
                <h2>Quản lí thành viên</h2>
              </div>
              <div className="col-sm-6">
                <button onClick={openAddMembershipModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm mới thành viên</span>
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
                <th>Trạng thái thanh toán</th>
                <th className="action-el">Hành động</th>
              </tr>
            </thead>
            <tbody>
            {filteredMembershipData.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage).map((membership, index) => (
  <tr key={index}>
    <td>
      <img src={`data:image/jpeg;base64,${membership.user.userAvatar}`} className="customer-avatar" />
      {membership.user.name}
    </td>
    <td>{membership.user.gender === "male" ? "Nam" : "Nữ"}</td>
    <td>{formatDate(membership.user.dob)}</td>
    <td>{membership.user.email}</td>
    <td>{membership.user.phone}</td>
    <td>{membership.user.address}</td>
    <td>
      {membership.gymRegistrations && membership.gymRegistrations.length > 0 
        ? (membership.gymRegistrations[0].isActive ? "Đã thanh toán" : "Chưa thanh toán")
        : "Chưa đăng ký"
      }
    </td>
    <td>
      <a
        href="#"
        onClick={(e) => {
          e.preventDefault();
          openDetailModal(membership);
        }}
        className="view"
      >
        <VisibilityIcon />
      </a>
    </td>
  </tr>
))}
            </tbody>
          </table>

          {/* Pagination */}
          <div className="clearfix-el">
            <div className="hint-text">
              Showing <b>{currentMemberships.length}</b> out of <b>{membershipData.length}</b> entries
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

      {/* Membership Modal */}
      <div ref={membershipModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="membershipForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentMembership ? "Sửa thông tin thành viên" : "Thêm thành viên"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                <div className="row">
                  <div className="form-group col">
                    Email <span className="icon-input">(*)</span>
                    <input type="email" className={`form-control ${errors.email ? "is-invalid" : ""}`} name="email" value={formData.email} onChange={handleInputChange} required />
                    {errors.email && <div className="error-message">{errors.email}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Gói đăng kí <span className="icon-input">(*)</span>
                    </label>
                    <select className="form-control form-select" name="gymMembershipId" value={formData.gymMembershipId || ""} onChange={handlePackageChange} required>
                      <option value="">Chọn gói</option>
                      {selectedPackages.map((option) => (
                        <option key={option.gymMembershipId} value={option.gymMembershipId}>
                          {option.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Chọn kiểu thanh toán <span className="icon-input">(*)</span>
                    </label>
                    <div className="radio-group">
                      <label>
                        <input type="radio" name="qrPayment" value="true" checked={formData.qrPayment === "true"} onChange={handleInputChange} />
                        Chuyển khoản
                      </label>
                      <label>
                        <input type="radio" name="qrPayment" value="false" checked={formData.qrPayment === "false"} onChange={handleInputChange} />
                        Tiền mặt
                      </label>
                    </div>
                  </div>
                </div>
              </div>

              <div className="modal-footer">
                <button type="button" className="btn btn-default" onClick={closeModal} disabled={isSubmitting} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
                  Hủy
                </button>

                <button type="submit" className="btn btn-success" disabled={isSubmitting}>
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
            <form id="deleteMembershipForm" onSubmit={handleDeleteMembership}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">Xóa thành viên</h4>
                <a type="button" className="close" onClick={closeDeleteModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                <p>Bạn có chắc chắn muốn xóa {membershipToDelete?.name}?</p>
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

      {/* QR Modal */}
      <div ref={showQrPicture} className="modal">
        <div className="modal-dialog modal-dialog-notify">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Thông báo</h4>
              <a type="button" className="close" onClick={closeQr}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              {/* Check if qrDataUrl exists and render the QR code */}
              {qrDataUrl ? (
                <div className="qr-code-container">
                  <img src={qrDataUrl} alt="QR Code" className="qr-code-image" />
                </div>
              ) : (
                <p>Không có mã QR để hiển thị.</p>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={closeQr}>
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

      {/* View detail Membership Modal */}
      <div ref={detailModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Thông tin chi tiết đăng ký gói tập</h4>
              <a type="button" className="close" onClick={closeDetailModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              {isLoadingUser ? (
                <div className="text-center">
                  <div className="spinner-border" role="status">
                    <span className="visually-hidden">Loading...</span>
                  </div>
                </div>
              ) : (
                <>
                  {/* Gym Memberships */}
                  {membershipDetails.gymMemberships.length > 0 && (
                    <div className="membership-section">
                      <h5>Gói thành viên</h5>
                      {membershipDetails.gymMemberships.map((gym, index) => (
                        <div key={index} className="membership-item">
                          <p>
                            <strong>Tên gói:</strong> {gym.name}
                          </p>
                          <p>
                            <strong>Ngày bắt đầu:</strong> {gym.startDate}
                          </p>
                          <p>
                            <strong>Ngày kết thúc:</strong> {gym.endDate}
                          </p>
                          <hr />
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Boxing Options */}
                  {membershipDetails.boxingOptions.length > 0 && (
                    <div className="membership-section">
                      <h5>Gói Boxing</h5>
                      {membershipDetails.boxingOptions.map((boxing, index) => (
                        <div key={index} className="membership-item">
                          <p>
                            <strong>Mô tả:</strong> {boxing.description}
                          </p>
                          <p>
                            <strong>Ngày bắt đầu:</strong> {boxing.startDate}
                          </p>
                          <p>
                            <strong>Ngày kết thúc:</strong> {boxing.endDate}
                          </p>
                          <hr />
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Rental Options */}
                  {membershipDetails.rentalOptions.length > 0 && (
                    <div className="membership-section">
                      <h5>Gói trainer gym</h5>
                      {membershipDetails.rentalOptions.map((rental, index) => (
                        <div key={index} className="membership-item">
                          <p>
                            <strong>Chi tiết:</strong> {rental.description}
                          </p>
                          <p>
                            <strong>Ngày bắt đầu:</strong> {rental.startDate}
                          </p>
                          <p>
                            <strong>Ngày kết thúc:</strong> {rental.endDate}
                          </p>
                          <hr />
                        </div>
                      ))}
                    </div>
                  )}

                  {membershipDetails.gymMemberships.length === 0 && membershipDetails.boxingOptions.length === 0 && membershipDetails.rentalOptions.length === 0 && <p className="text-center">Không có gói tập nào được đăng ký</p>}
                </>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={closeDetailModal}>
                Đóng
              </button>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default ManageMembership;
