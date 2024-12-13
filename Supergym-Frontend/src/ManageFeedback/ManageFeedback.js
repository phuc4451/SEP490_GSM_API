import React, { useEffect, useState, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageFeedback.css";
import VisibilityIcon from "@mui/icons-material/Visibility";
import SearchIcon from "@mui/icons-material/Search";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import StarIcon from "@mui/icons-material/Star";
import { useNavigate } from "react-router-dom";
import { getRole, Logout } from "../utils/authUtils";
import LoadingSpinner from "../utils/LoadingOverlay";

const ManageFeedback = () => {
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

  // Fetch all feedback with user data
  const fetchAllFeedback = async () => {
    const token = localStorage.getItem("token");
    setIsDataLoading(true);
    
    try {
      const feedbackResponse = await axios.get("http://localhost:5000/api/Feedback", {
        headers: { Authorization: `Bearer ${token}` },
      });

      const feedbacksWithUsers = await Promise.all(
        feedbackResponse.data.map(async (feedback) => {
          try {
            const userResponse = await axios.get(
              `http://localhost:5000/api/Users/${feedback.userId}`,
              { headers: { Authorization: `Bearer ${token}` } }
            );
            return { ...feedback, user: userResponse.data };
          } catch (error) {
            console.error(`Error fetching user data for user ${feedback.userId}:`, error);
            return { ...feedback, user: { name: "Unknown", email: "Unknown" } };
          }
        })
      );

      setFeedbackList(feedbacksWithUsers);
      setSearchMessage("");
    } catch (error) {
      console.error("Error fetching feedback data:", error);
      setSearchMessage("Có lỗi xảy ra khi tải dữ liệu");
    } finally {
      setIsDataLoading(false);
    }
  };

  // Initial data fetch
  useEffect(() => {
    fetchAllFeedback();
  }, []);

  // Search functionality
  const handleSearch = async () => {
    if (!searchQuery.trim()) {
      await fetchAllFeedback();
      return;
    }

    setIsSearching(true);
    const token = localStorage.getItem("token");

    try {
      const response = await axios.get(
        `http://localhost:5000/api/Feedback/search`,
        {
          params: { email: searchQuery },
          headers: { Authorization: `Bearer ${token}` },
        }
      );

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

  // Filter feedbacks based on month and rating
  const filteredFeedbacks = feedbackList.filter((feedback) => {
    const monthMatch = !selectedMonth || 
      new Date(feedback.submittedAt).getMonth() + 1 === parseInt(selectedMonth);
    const ratingMatch = !selectedRating || 
      feedback.rating === parseInt(selectedRating);
    return monthMatch && ratingMatch;
  });

  // Calculate average rating
  const calculateAverageRating = () => {
    if (feedbackList.length === 0) return 0;
    const totalRating = feedbackList.reduce((sum, feedback) => sum + feedback.rating, 0);
    return (totalRating / feedbackList.length).toFixed(1);
  };

  // Pagination calculations
  const totalPages = Math.ceil(filteredFeedbacks.length / itemsPerPage);
  const currentPageData = filteredFeedbacks.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  // Modal handling
  const openViewFeedbackModal = async (feedback) => {
    setIsLoadingUser(true);
    const token = localStorage.getItem("token");

    try {
      const userResponse = await axios.get(
        `http://localhost:5000/api/Users/GetUserById/${feedback.userId}`,
        { headers: { Authorization: `Bearer ${token}` } }
      );
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
    feedbackModalRef.current.style.display = "none";
    feedbackModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentFeedback(null);
    setCurrentUserData(null);
  };

  // Pagination handling
  const handlePageChange = (pageNumber) => {
    if (pageNumber < 1 || pageNumber > totalPages) return;
    setCurrentPage(pageNumber);
  };
  return (
    <>
      <Header />
      {isDataLoading ? <Preloader /> : null}

      {isLoadingUser && <LoadingSpinner isLoading={true} />}

      <div className="user-select">
        <h1>Quản lý phản hồi từ hệ thống SUPER GYM</h1>
        <div className="select-search-container">
          <div className="search-container">
            <input
              type="text"
              id="searchUser"
              className="form-control"
              placeholder="Tìm kiếm theo email..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyPress={(e) => {
                if (e.key === "Enter") {
                  handleSearch();
                }
              }}
            />
            {/* {searchMessage && (
              <div className="alert alert-info mt-2" role="alert">
                {searchMessage}
              </div>
            )} */}
            <span className="search-icon" style={{ cursor: "pointer" }} onClick={handleSearch}>
              <SearchIcon />
            </span>
            {isSearching && <LoadingSpinner isLoading={true} />}
          </div>

          <select className="form-control form-select" id="selectMonth" value={selectedMonth} onChange={(e) => setSelectedMonth(e.target.value)}>
            <option value="">Tất cả các tháng</option>
            {Array.from({ length: 12 }, (_, index) => (
              <option key={index + 1} value={index + 1}>{`Tháng ${index + 1}`}</option>
            ))}
          </select>

          <select className="form-control form-select" id="selectRating" value={selectedRating} onChange={(e) => setSelectedRating(e.target.value)}>
            <option value="">Tất cả đánh giá ⭐</option>
            {Array.from({ length: 5 }, (_, index) => (
              <option key={index + 1} value={index + 1}>
                {index + 1} ⭐
              </option>
            ))}
          </select>

          <span>Phản hồi trung bình: {calculateAverageRating()} ⭐</span>
        </div>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Quản lí phản hồi khách hàng</h2>
              </div>
            </div>
          </div>
          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                <th>Nội dung</th>
                <th>Đánh giá</th>
                <th>Ngày đánh giá</th>
                <th>Hành động</th>
              </tr>
            </thead>
            <tbody>
              {currentPageData.map((feedback) => (
                <tr key={feedback.feedbackId} className={feedback.rating <= 3 ? "low-rating" : ""}>
                  <td>{feedback.message}</td>
                  <td>
                    {Array.from({ length: 5 }, (_, index) => (
                      <StarIcon
                        key={index}
                        style={{
                          color: index < feedback.rating ? "#FFD700" : "#E0E0E0",
                        }}
                      />
                    ))}
                  </td>
                  <td>{new Date(feedback.submittedAt).toLocaleDateString("vi-VN")}</td>
                  <td>
                    <a
                      href="#"
                      onClick={(e) => {
                        e.preventDefault();
                        openViewFeedbackModal(feedback);
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

          <div className="clearfix-el">
            <div className="hint-text">
              Hiển thị <b>{currentPageData.length}</b> trong <b>{filteredFeedbacks.length}</b> kết quả
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

      {/* Feedback Modal */}
      <div ref={feedbackModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Phản hồi chi tiết</h4>
              <a type="button" className="close" onClick={closeModal}>
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
                currentFeedback &&
                currentUserData && (
                  <>
                    <p>
                      <strong>ID người phản hồi:</strong> {currentUserData.userId}
                    </p>
                    <p>
                      <strong>Tên:</strong> {currentUserData.name}
                    </p>
                    <p>
                      <strong>Email:</strong> {currentUserData.email}
                    </p>
                    <p>
                      <strong>Nội dung:</strong> {currentFeedback.message}
                    </p>
                    <p>
                      <strong>Phản hồi:</strong> {currentFeedback.rating}⭐
                    </p>
                    <p>
                      <strong>Ngày gửi:</strong> {new Date(currentFeedback.submittedAt).toLocaleDateString("vi-VN")}
                    </p>
                  </>
                )
              )}
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
      <div className="modal-overlay" onClick={closeModal}></div>
    </>
  );
};

export default ManageFeedback;