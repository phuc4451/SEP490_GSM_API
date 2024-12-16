import React, { useEffect, useState, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageSalary.css";
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

const ManageSalary = () => {
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
  const [salaryConfigs, setSalaryConfigs] = useState([]);
  const SalaryModalRef = useRef(null);
  const errorModalRef = useRef(null);
  const successModalRef = useRef(null);
  const [errors, setErrors] = useState({});
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  const [formData, setFormData] = useState({
    salaryType: "trainer",
    baseSalary: "",
    perShiftSalary: "",
    perSlotSalary: "",
    finePerLate: "",
    finePerAbsence: "",
  });
  const [currentShift, setCurrentShift] = useState(null); // for editing Staff

  const fetchAllSalaryConfig = async () => {
    const token = localStorage.getItem("token");
    setIsDataLoading(true);

    try {
      const response = await axios.get("http://localhost:5000/api/Salary/GetSalaryConfigs", {
        headers: { Authorization: `Bearer ${token}` },
      });
      setSalaryConfigs(response.data);
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
    fetchAllSalaryConfig();
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
  // const paginatedData = salaryConfigs.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);
  const totalPages = Math.ceil(salaryConfigs.length / itemsPerPage);
  const currentPageData = salaryConfigs.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);

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
    SalaryModalRef.current.style.display = "none";
    SalaryModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
  };

  // Pagination handling
  const handlePageChange = (pageNumber) => {
    if (pageNumber < 1 || pageNumber > totalPages) return;
    setCurrentPage(pageNumber);
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(amount);
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
    const numValue = Number(value);

    switch (name) {
      case "baseSalary":
        if (!value) {
          newErrors.baseSalary = "Vui lòng nhập lương cơ bản";
        } else if (numValue <= 0) {
          newErrors.baseSalary = "Lương cơ bản phải lớn hơn 0";
        } else {
          delete newErrors.baseSalary;
        }
        break;
      case "perShiftSalary":
        if (formData.salaryType === "staff") {
          if (!value) {
            newErrors.perShiftSalary = "Vui lòng nhập lương theo ca";
          } else if (numValue <= 0) {
            newErrors.perShiftSalary = "Lương theo ca phải lớn hơn 0";
          } else {
            delete newErrors.perShiftSalary;
          }
        }
        break;
      case "perSlotSalary":
        if (formData.salaryType === "trainer") {
          if (!value) {
            newErrors.perSlotSalary = "Vui lòng nhập lương theo slot";
          } else if (numValue <= 0) {
            newErrors.perSlotSalary = "Lương theo slot phải lớn hơn 0";
          } else {
            delete newErrors.perSlotSalary;
          }
        }
        break;
      case "finePerLate":
        if (!value) {
          newErrors.finePerLate = "Vui lòng nhập mức phạt đi muộn";
        } else if (numValue <= 0) {
          newErrors.finePerLate = "Mức phạt đi muộn phải lớn hơn 0";
        } else {
          delete newErrors.finePerLate;
        }
        break;
      case "finePerAbsence":
        if (!value) {
          newErrors.finePerAbsence = "Vui lòng nhập mức phạt vắng mặt";
        } else if (numValue <= 0) {
          newErrors.finePerAbsence = "Mức phạt vắng mặt phải lớn hơn 0";
        } else {
          delete newErrors.finePerAbsence;
        }
        break;
      default:
        break;
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const openAddSalaryModal = () => {
    setFormData({
      salaryType: "trainer",
      baseSalary: "",
      perShiftSalary: "",
      perSlotSalary: "",
      finePerLate: "",
      finePerAbsence: "",
    });
    setErrors({});
    SalaryModalRef.current.style.display = "block";
    SalaryModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const validateForm = () => {
    let isValid = true;
    const newErrors = {};

    // Validate base salary
    if (!formData.baseSalary || Number(formData.baseSalary) <= 0) {
      newErrors.baseSalary = "Lương cơ bản phải lớn hơn 0";
      isValid = false;
    }

    // Validate per shift salary for staff
    if (formData.salaryType === "staff") {
      if (!formData.perShiftSalary || Number(formData.perShiftSalary) <= 0) {
        newErrors.perShiftSalary = "Lương theo ca phải lớn hơn 0";
        isValid = false;
      }
    }

    // Validate per slot salary for trainer
    if (formData.salaryType === "trainer") {
      if (!formData.perSlotSalary || Number(formData.perSlotSalary) <= 0) {
        newErrors.perSlotSalary = "Lương theo slot phải lớn hơn 0";
        isValid = false;
      }
    }

    // Validate fines
    if (!formData.finePerLate || Number(formData.finePerLate) <= 0) {
      newErrors.finePerLate = "Mức phạt đi muộn phải lớn hơn 0";
      isValid = false;
    }

    if (!formData.finePerAbsence || Number(formData.finePerAbsence) <= 0) {
      newErrors.finePerAbsence = "Mức phạt vắng mặt phải lớn hơn 0";
      isValid = false;
    }

    setErrors(newErrors);
    return isValid;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!validateForm()) {
      showErrorModal("Vui lòng kiểm tra lại các trường thông tin");
      return;
    }

    const token = localStorage.getItem("token");
    setIsSaving(true);

    try {
      const salaryConfig = {
        configurationId: "string",
        baseSalary: Number(formData.baseSalary),
        perShiftSalary: formData.salaryType === "staff" ? Number(formData.perShiftSalary) : 0,
        perSlotSalary: formData.salaryType === "trainer" ? Number(formData.perSlotSalary) : 0,
        finePerLate: Number(formData.finePerLate),
        finePerAbsence: Number(formData.finePerAbsence),
      };

      const response = await axios.post("http://localhost:5000/api/Salary/AddSalaryConfig", salaryConfig, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.data) {
        if (response.data.success) {  // Giả sử API trả về field success
          closeModal();
          showSuccessModal(response.data || "Thêm cấu hình lương thành công");
        } else {
          showErrorModal(response.data || "Có lỗi xảy ra khi thêm cấu hình lương");
        }
      }
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
        <h1>Quản lý lương nhân viên từ hệ thống SUPER GYM</h1>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Danh sách lương</h2>
              </div>
              <div className="col-sm-6">
                <button onClick={openAddSalaryModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm mới mức lương</span>
                </button>
              </div>
            </div>
          </div>
          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                {/* <th>ID Cấu hình</th> */}
                <th>Lương cơ bản</th>
                <th>Lương theo ca</th>
                <th>Lương theo slot</th>
                <th>Phạt đi muộn</th>
                <th>Phạt vắng mặt</th>
              </tr>
            </thead>
            <tbody>
              {salaryConfigs.map((config) => (
                <tr key={config.configurationId}>
                  {/* <td>{config.configurationId}</td> */}
                  <td>{formatCurrency(config.baseSalary)}</td>
                  <td>{formatCurrency(config.perShiftSalary)}</td>
                  <td>{formatCurrency(config.perSlotSalary)}</td>
                  <td>{formatCurrency(config.finePerLate)}</td>
                  <td>{formatCurrency(config.finePerAbsence)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="clearfix-el">
            <div className="hint-text">
              Hiển thị <b>{currentPageData.length}</b> trong <b>{salaryConfigs.length}</b> kết quả
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

      <div ref={SalaryModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">Thêm cấu hình lương mới</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* Salary type selector */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Loại nhân viên <span className="icon-input">(*)</span>
                    </label>
                    <div>
                      <label>
                        <input type="radio" name="salaryType" value="trainer" checked={formData.salaryType === "trainer"} onChange={handleInputChange} />
                        Trainer
                      </label>
                      <label className="ms-3">
                        <input type="radio" name="salaryType" value="staff" checked={formData.salaryType === "staff"} onChange={handleInputChange} />
                        Staff
                      </label>
                    </div>
                  </div>

                  {/* Base salary input */}
                  <div className="form-group col">
                    <label>
                      Lương cơ bản <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${errors.baseSalary ? "is-invalid" : ""}`} name="baseSalary" value={formData.baseSalary} onChange={handleInputChange} required />
                    {errors.baseSalary && <div className="invalid-feedback">{errors.baseSalary}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Phạt đi muộn <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${errors.finePerLate ? "is-invalid" : ""}`} name="finePerLate" value={formData.finePerLate} onChange={handleInputChange} required />
                    {errors.finePerLate && <div className="invalid-feedback">{errors.finePerLate}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Phạt vắng mặt <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${errors.finePerAbsence ? "is-invalid" : ""}`} name="finePerAbsence" value={formData.finePerAbsence} onChange={handleInputChange} required />
                    {errors.finePerAbsence && <div className="invalid-feedback">{errors.finePerAbsence}</div>}
                  </div>
                </div>

                {/* Conditional rendering based on salary type */}
                {formData.salaryType === "trainer" ? (
                  <div className="row">
                    <div className="form-group col">
                      <label>
                        Lương theo slot <span className="icon-input">(*)</span>
                      </label>
                      <input type="number" className={`form-control ${errors.perSlotSalary ? "is-invalid" : ""}`} name="perSlotSalary" value={formData.perSlotSalary} onChange={handleInputChange} required />
                      {errors.perSlotSalary && <div className="invalid-feedback">{errors.perSlotSalary}</div>}
                    </div>
                  </div>
                ) : (
                  <div className="row">
                    <div className="form-group col">
                      <label>
                        Lương theo ca <span className="icon-input">(*)</span>
                      </label>
                      <input type="number" className={`form-control ${errors.perShiftSalary ? "is-invalid" : ""}`} name="perShiftSalary" value={formData.perShiftSalary} onChange={handleInputChange} required />
                      {errors.perShiftSalary && <div className="invalid-feedback">{errors.perShiftSalary}</div>}
                    </div>
                  </div>
                )}
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
              <p>thêm cấu hình lương thành công!</p>
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

export default ManageSalary;
