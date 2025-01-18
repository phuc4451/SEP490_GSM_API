import React, { useEffect, useState, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageShift.css";
import VisibilityIcon from "@mui/icons-material/Visibility";
import SearchIcon from "@mui/icons-material/Search";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import StarIcon from "@mui/icons-material/Star";
import { useNavigate } from "react-router-dom";
import { getRole, Logout } from "../utils/authUtils";
import LoadingSpinner from "../utils/LoadingOverlay";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";

const ManageShift = () => {
  const navigate = useNavigate();

  // Authentication check
  useEffect(() => {
    const userRole = getRole();
    if (userRole !== "admin") {
      Logout();
      navigate("/login");
      return;
    }
  }, [navigate]);

  // State declarations
  const [feedbackList, setFeedbackList] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedMonth, setSelectedMonth] = useState("");
  const [selectedRating, setSelectedRating] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [searchMessage, setSearchMessage] = useState("");
  const [isSearching, setIsSearching] = useState(false);
  const [currentFeedback, setCurrentFeedback] = useState(null);
  const [currentUserData, setCurrentUserData] = useState(null);
  const [isLoadingUser, setIsLoadingUser] = useState(false);
  const feedbackModalRef = useRef(null);
  const itemsPerPage = 13;
  const [shiftList, setShiftList] = useState([]);
  const [errors, setErrors] = useState({});
  const ShiftModalRef = useRef(null);
  const successModalRef = useRef(null);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const errorModalRef = useRef(null);
  const [isSaving, setIsSaving] = useState(false);

  const [formData, setFormData] = useState({
    shiftId: "",
    shiftName: "",
    startTime: "",
    endTime: "",
    location: "",
    shiftType: "",
  });
  const fetchAllShifts = async () => {
    const token = localStorage.getItem("token");
    setIsDataLoading(true);

    try {
      const response = await axios.get("http://localhost:5000/api/Salary/GetShifts", {
        headers: { Authorization: `Bearer ${token}` },
      });
      setShiftList(response.data);
      setSearchMessage("");
    } catch (error) {
      console.error("Error fetching shift data:", error);
      setSearchMessage("Có lỗi xảy ra khi tải dữ liệu");
    } finally {
      setIsDataLoading(false);
    }
  };

  // Initial data fetch
  useEffect(() => {
    fetchAllShifts();
  }, []);

  // Search functionality
  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      //   await fetchAllFeedback();
      return;
    }

    setIsSearching(true);
    const token = localStorage.getItem("token");

    try {
      const response = await axios.get(`http://localhost:5000/api/Feedback/search`, {
        params: { email: searchQuery },
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.data.data) {
        setFeedbackList(response.data.data);
        setSearchMessage(response.data.message);
        setCurrentPage(1);
      }
    } catch (error) {
      if (error.response?.status === 404) {
        setFeedbackList([]);
        setSearchMessage(error.response.data.message);
      } else {
        console.error("Error searching feedback:", error);
        setSearchMessage("Có lỗi xảy ra khi tìm kiếm");
      }
    } finally {
      setIsSearching(false);
    }
  };

  // Pagination calculations
  const totalPages = Math.ceil(shiftList.length / itemsPerPage);
  const currentPageData = shiftList.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);

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
      case "shiftName":
        if (!value.trim()) {
          newErrors.shiftName = "Vui lòng nhập tên ca làm";
        } else {
          delete newErrors.shiftName;
        }
        break;
      // ... các case khác
    }
    setErrors(newErrors);
  };

  const validateForm = () => {
    const newErrors = {};
    if (!formData.idCard || !/^\d+$/.test(formData.idCard) || formData.idCard.length !== 12) {
      newErrors.idCard = "Số căn cước phải là số và có đúng 12 chữ số.";
    }

    if (!formData.gender) {
      newErrors.gender = "Vui lòng chọn giới tính.";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // If no errors, form is valid
  };

  // Modal handling
  const openViewFeedbackModal = async (feedback) => {
    setIsLoadingUser(true);
    const token = localStorage.getItem("token");

    try {
      const userResponse = await axios.get(`http://localhost:5000/api/Users/GetUserById/${feedback.userId}`, { headers: { Authorization: `Bearer ${token}` } });
      setCurrentUserData(userResponse.data);
      setCurrentFeedback(feedback);
    } catch (error) {
      console.error(`Error fetching user data:`, error);
      setCurrentUserData({ name: "Unknown", email: "Unknown" });
    } finally {
      setIsLoadingUser(false);
      feedbackModalRef.current.style.display = "block";
      feedbackModalRef.current.classList.add("active");
      document.querySelector(".modal-overlay").style.display = "block";
    }
  };

  const closeModal = () => {
    ShiftModalRef.current.style.display = "none";
    ShiftModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
  };

  const openAddShiftModal = () => {
    setFormData({
      shiftId: "",
      shiftName: "",
      startTime: "",
      endTime: "",
      location: "",
      shiftType: "",
    });
    setErrors({});
    ShiftModalRef.current.style.display = "block";
    ShiftModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  // Pagination handling
  const handlePageChange = (pageNumber) => {
    if (pageNumber < 1 || pageNumber > totalPages) return;
    setCurrentPage(pageNumber);
  };

  const formatDateTime = (dateString) => {
    // Lấy trực tiếp từ chuỗi UTC mà không cần chuyển đổi múi giờ
    const utcDate = new Date(dateString);

    // Format ngày tháng và giờ
    const day = String(utcDate.getUTCDate()).padStart(2, "0");
    const month = String(utcDate.getUTCMonth() + 1).padStart(2, "0");
    const year = utcDate.getUTCFullYear();
    const hours = String(utcDate.getUTCHours()).padStart(2, "0");
    const minutes = String(utcDate.getUTCMinutes()).padStart(2, "0");

    // Trả về định dạng "DD/MM/YYYY HH:mm"
    return `${day}/${month}/${year} ${hours}:${minutes}`;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Kiểm tra tên ca làm
    if (!formData.shiftName.trim()) {
      setErrors((prev) => ({ ...prev, shiftName: "Vui lòng nhập tên ca làm" }));
      return;
    }

    // Kiểm tra thời gian
    const [checkStartHours, checkStartMinutes] = formData.startTime.split(":");
    const [checkEndHours, checkEndMinutes] = formData.endTime.split(":");

    const startMinutesTotal = parseInt(checkStartHours) * 60 + parseInt(checkStartMinutes);
    const endMinutesTotal = parseInt(checkEndHours) * 60 + parseInt(checkEndMinutes);

    if (startMinutesTotal >= endMinutesTotal) {
      showErrorModal("Giờ bắt đầu phải nhỏ hơn giờ kết thúc");
      return;
    }

    setIsSaving(true);

    // Lấy ngày hiện tại ở múi giờ local
    const currentDate = new Date();
    const year = currentDate.getFullYear();
    const month = currentDate.getMonth();
    const date = currentDate.getDate();

    // Tách giờ và phút từ input
    const [startHours, startMinutes] = formData.startTime.split(":");
    const [endHours, endMinutes] = formData.endTime.split(":");

    // Tạo datetime cho startTime và endTime với ngày hiện tại
    const startDateTime = new Date(year, month, date, parseInt(startHours), parseInt(startMinutes));
    const endDateTime = new Date(year, month, date, parseInt(endHours), parseInt(endMinutes));

    // Format datetime thủ công để tránh chuyển đổi UTC
    const formatDate = (date) => {
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, "0");
      const day = String(date.getDate()).padStart(2, "0");
      const hours = String(date.getHours()).padStart(2, "0");
      const minutes = String(date.getMinutes()).padStart(2, "0");
      const seconds = String(date.getSeconds()).padStart(2, "0");

      return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}.000Z`;
    };

    const addShift = {
      shiftId: "string",
      shiftName: formData.shiftName,
      startTime: formatDate(startDateTime),
      endTime: formatDate(endDateTime),
      location: "string",
      shiftType: "string",
    };

    try {
      const token = localStorage.getItem("token");
      setIsSaving(true);
      const response = await axios.post(`http://localhost:5000/api/Salary/AddShift`, addShift, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      // Kiểm tra và xử lý response
      if (response.status === 200) {
        closeModal();
        showSuccessModal("Ca làm việc được lưu thành công");
      } else {
        showErrorModal("Có lỗi xảy ra khi lưu ca làm việc");
      }
      // }
    } catch (error) {
      console.error("Error saving Staff:", error);
      // Xử lý các loại lỗi khác nhau
      if (error.response) {
        // Server trả về response với status code nằm ngoài range 2xx
        showErrorModal(error.response.data || "Có lỗi xảy ra từ server");
      } else if (error.request) {
        // Request được gửi nhưng không nhận được response
        showErrorModal("Không thể kết nối đến server");
      } else {
        // Có lỗi khi setting up request
        showErrorModal(error.message || "Có lỗi xảy ra");
      }
    } finally {
      setIsSaving(false);
    }
};
  return (
    <>
      <Header />
      {isDataLoading ? <Preloader /> : null}

      {isSaving && <LoadingSpinner isLoading={true} />}

      <div className="user-select">
        <h1>Quản lý ca làm việc từ hệ thống SUPER GYM</h1>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Danh sách ca làm việc</h2>
              </div>
              <div className="col-sm-6">
                <button onClick={openAddShiftModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm mới ca làm</span>
                </button>
              </div>
            </div>
          </div>
          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                {/* <th>ID Ca</th> */}
                <th>Tên ca</th>
                <th>Thời gian bắt đầu</th>
                <th>Thời gian kết thúc</th>
                {/* <th>Địa điểm</th> */}
                {/* <th>Loại ca</th> */}
              </tr>
            </thead>
            <tbody>
              {currentPageData.map((shift) => (
                <tr key={shift.shiftId}>
                  <td>{shift.shiftName}</td>
                  <td>{formatDateTime(shift.startTime)}</td>
                  <td>{formatDateTime(shift.endTime)}</td>
                  {/* <td>{shift.location}</td>
                  <td>{shift.shiftType}</td> */}
                </tr>
              ))}
            </tbody>
          </table>

          <div className="clearfix-el">
            <div className="hint-text">
              Hiển thị <b>{currentPageData.length}</b> trong <b>{shiftList.length}</b> kết quả
            </div>
            <ul className="pagination">
              <li className={`page-item ${currentPage === 1 ? "disabled" : ""}`}>
                <a
                  href="#"
                  onClick={(e) => {
                    e.preventDefault();
                    handlePageChange(currentPage - 1);
                  }}
                  className="page-link"
                >
                  <ChevronLeftIcon />
                </a>
              </li>
              {Array.from({ length: totalPages }, (_, index) => (
                <li key={index} className={`page-item ${currentPage === index + 1 ? "active" : ""}`}>
                  <a
                    href="#"
                    onClick={(e) => {
                      e.preventDefault();
                      handlePageChange(index + 1);
                    }}
                    className="page-link"
                  >
                    {index + 1}
                  </a>
                </li>
              ))}
              <li className={`page-item ${currentPage === totalPages ? "disabled" : ""}`}>
                <a
                  href="#"
                  onClick={(e) => {
                    e.preventDefault();
                    handlePageChange(currentPage + 1);
                  }}
                  className="page-link"
                >
                  <ChevronRightIcon />
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>

      <div ref={ShiftModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">Thêm ca làm</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* Salary type selector */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên ca làm <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.shiftName ? "is-invalid" : ""}`} name="shiftName" value={formData.shiftName} onChange={handleInputChange} required />
                    {errors.shiftName && <div className="invalid-feedback">{errors.shiftName}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Giờ bắt đầu <span className="icon-input">(*)</span>
                    </label>
                    <input type="time" className="form-control" name="startTime" value={formData.startTime} onChange={handleInputChange} required />
                  </div>

                  <div className="form-group col">
                    <label>
                      Giờ kết thúc <span className="icon-input">(*)</span>
                    </label>
                    <input type="time" className="form-control" name="endTime" value={formData.endTime} onChange={handleInputChange} required />
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

      {/* Modal Overlay */}
      <div className="modal-overlay" onClick={closeModal}></div>

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
              <p>thêm ca làm thành công!</p>
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

export default ManageShift;
