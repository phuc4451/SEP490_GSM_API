import React, { useEffect, useState, useRef } from "react";
import axios from "axios";
import Header from "../Header/Header";
import Preloader from "../Preloader/Preloader";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageFeedback.css";
import VisibilityIcon from "@mui/icons-material/Visibility";
import SearchIcon from "@mui/icons-material/Search";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import DeleteIcon from "@mui/icons-material/Delete";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloseIcon from "@mui/icons-material/Close";
import StarIcon from "@mui/icons-material/Star";
import { useNavigate } from "react-router-dom";
import { getRole,Logout } from '../utils/authUtils';
const ManageFeedback = () => {
  const navigate = useNavigate();
  useEffect(() => {
    const userRole = getRole();
    if (userRole !== 'admin') {
      Logout();
      navigate('/login'); // or redirect to login
      return;
    }
  }, [navigate]);
  const [feedbackList, setFeedbackList] = useState([]);
  const [feedbackListUserInfor, setFeedbackListUserInfor] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedRole, setSelectedRole] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("");
  const [selectedGender, setSelectedGender] = useState("");
  const [selectedMonth, setSelectedMonth] = useState("");
  const [selectedRating, setSelectedRating] = useState("");

  const [users, setUsers] = useState({});
  const itemsPerPage = 8;
  const [searchQuery, setSearchQuery] = useState("");

  const feedbackModalRef = useRef(null);
  const [currentFeedback, setCurrentFeedback] = useState(null);

  // useEffect(() => {
  //   const fetchFeedbacks = async () => {
  //     const token = localStorage.getItem("token");
  //     try {
  //       const response = await axios.get("http://localhost:5000/api/Feedback", {
  //         headers: { Authorization: `Bearer ${token}` },
  //       });
  //       setFeedbackList(response.data);

  //       const uniqueUserIds = [...new Set(response.data.map((fb) => fb.userId))];
  //       await Promise.all(
  //         uniqueUserIds.map(async (userId) => {
  //           const userResponse = await axios.get(`http://localhost:5000/api/Users/${userId}`, {
  //             headers: { Authorization: `Bearer ${token}` },
  //           });
  //           setUsers((prevUsers) => ({
  //             ...prevUsers,
  //             [userId]: userResponse.data,
  //           }));
  //         })
  //       );
  //     } catch (error) {
  //       console.error("Error fetching data:", error);
  //     } finally {
  //       setIsDataLoading(false);
  //     }
  //   };
  //   fetchFeedbacks();
  // }, []);

  useEffect(() => {
    const fetchFeedbacksWithUserInfor = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Feedback/FeedbackWithUserInfo", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setFeedbackListUserInfor(response.data);
      } catch (error) {
        console.error("Error fetching data:", error);
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchFeedbacksWithUserInfor();
  }, []);

  // const totalPages = Math.ceil(feedbackListUserInfor.length / itemsPerPage);
  // const currentFeedbacks = feedbackList.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);

  // const openViewFeedbackModal = (feedback) => {
  //   setCurrentFeedback(feedback);
  //   feedbackModalRef.current.style.display = "block";
  //   document.querySelector(".modal-overlay").style.display = "block";
  // };

  const currentFeedbacks = feedbackListUserInfor
    .filter((feedback) => {
      if (selectedMonth && new Date(feedback.submittedAt).getMonth() + 1 !== parseInt(selectedMonth)) return false;
      if (selectedRating && feedback.rating !== parseInt(selectedRating)) return false;
      return true;
    })
    .slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);

  const openViewFeedbackModal = (feedback) => {
    setCurrentFeedback(feedback);
    feedbackModalRef.current.style.display = "block";
    feedbackModalRef.current.classList.add("active"); // Thêm class 'active'

    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeModal = () => {
    feedbackModalRef.current.style.display = "none";
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentFeedback(null);
  };

  const handlePageChange = (pageNumber) => {
    setCurrentPage(pageNumber);
  };

  const handleSearchChange = (e) => {
    setSearchQuery(e.target.value); // Cập nhật nội dung tìm kiếm khi người dùng nhập
  };
// First, combine all filters (search, rating, month)
const filteredUserData = feedbackListUserInfor.filter((feedback) => {
  // Search by email
  const emailMatch = feedback.user.email.toLowerCase().includes(searchQuery.toLowerCase());
  
  // Filter by month (if selected)
  const monthMatch = !selectedMonth || 
    new Date(feedback.submittedAt).getMonth() + 1 === parseInt(selectedMonth);
  
  // Filter by rating (if selected)
  const ratingMatch = !selectedRating || 
    feedback.rating === parseInt(selectedRating);

  return emailMatch && monthMatch && ratingMatch;
});

  const deleteFeedback = async (feedbackId) => {
    const token = localStorage.getItem("token");
    try {
      await axios.delete(`http://localhost:5000/api/Feedback/${feedbackId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      setFeedbackList((prev) => prev.filter((fb) => fb.feedbackId !== feedbackId));
    } catch (error) {
      console.error("Error deleting feedback:", error);
    }
  };

  const calculateAverageRating = () => {
    if (feedbackListUserInfor.length === 0) return 0;
    const totalRating = feedbackListUserInfor.reduce((sum, feedback) => sum + feedback.rating, 0);
    return (totalRating / feedbackListUserInfor.length).toFixed(1); // Làm tròn đến một chữ số thập phân
  };

  const averageRating = calculateAverageRating();
// Pagination calculation for filtered data
const totalPages = Math.ceil(filteredUserData.length / itemsPerPage);
const currentPageData = filteredUserData.slice(
  (currentPage - 1) * itemsPerPage, 
  currentPage * itemsPerPage
);
  return (
    <>
      <Header />
      {isDataLoading ? <Preloader /> : null}

      <div className="user-select">
        <h1>Quản lý phản hồi từ hệ thống SUPER GYM</h1>
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

          <select className="form-control form-select" id="selectMonth" value={selectedMonth} onChange={(e) => setSelectedMonth(e.target.value)}>
            <option value="">Chọn tháng</option>
            {Array.from({ length: 12 }, (_, index) => (
              <option key={index + 1} value={index + 1}>{`Tháng ${index + 1}`}</option>
            ))}
          </select>

          <select className="form-control form-select" id="selectRating" value={selectedRating} onChange={(e) => setSelectedRating(e.target.value)}>
            <option value="">Chọn số ⭐</option>
            {Array.from({ length: 5 }, (_, index) => (
              <option key={index + 1} value={index + 1}>
                {index + 1} ⭐
              </option>
            ))}
          </select>

          <span>Phản hồi trung bình: {averageRating} ⭐</span>
        </div>
      </div>

      <div className="table-container">
        <div className="table-wrapper">
          <div className="table-title">
            <div className="row">
              <div className="col-sm-6">
                <h2>Quản lí phản hồi khách hàng</h2>
              </div>
              {/* <div className="col-sm-6">
                <button className="btn btn-success">
                  <AddCircleOutlineIcon /> Thêm mới
                </button>
              </div> */}
            </div>
          </div>
          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                <th>Người dùng</th>
                <th>Nội dung</th>
                <th>Đánh giá</th>
                <th>Ngày đánh giá</th>
                <th>Hành động</th>
              </tr>
            </thead>
            <tbody>
  {currentPageData.map((feedback) => {
    const rowClass = feedback.rating <= 3 ? "low-rating" : "";
    
    return (
      <tr key={feedback.feedbackId} className={rowClass}>
        <td>
          <div className="user-info">
            <img 
              src={`data:image/jpeg;base64,${feedback.user.userAvatar}`} 
              alt="User Avatar" 
              className="customer-avatar" 
            />
            <span>{feedback.user.name}</span>
          </div>
        </td>
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
    );
  })}
</tbody>
          </table>

          <div className="clearfix-el">
            <div className="hint-text">
              Showing <b>{currentFeedbacks.length}</b> out of <b>{filteredUserData.length}</b> entries
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

      {/* Feedback Modal */}
      <div ref={feedbackModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h4 className="modal-title text-center mx-auto">Feedback Details</h4>
              <a type="button" className="close" onClick={closeModal}>
                <CloseIcon />
              </a>
            </div>
            <div className="modal-body">
              {currentFeedback && (
                <>
                  <p>
                    <strong>ID người phản hồi:</strong> {currentFeedback.user.userId}
                  </p>
                  <p>
                    <strong>Tên:</strong> {currentFeedback.user.name}
                  </p>
                  <p>
                    <strong>Email:</strong> {currentFeedback.user.email}
                  </p>
                  <p>
                    <strong>Nội dung:</strong> {currentFeedback.message}
                  </p>
                  <p>
                    <strong>Phản hồi:</strong> {currentFeedback.rating}⭐
                  </p>
                  <p>
                    <strong>Submitted At:</strong> {new Date(currentFeedback.submittedAt).toLocaleDateString("vi-VN")}
                  </p>
                </>
              )}
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={closeModal}>
                Close
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
