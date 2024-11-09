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

const ManageFeedback = () => {
  const [feedbackList, setFeedbackList] = useState([]);
  const [isDataLoading, setIsDataLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedRole, setSelectedRole] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("");
  const [selectedGender, setSelectedGender] = useState("");
  const [selectedMonth, setSelectedMonth] = useState("");
  const [selectedRating, setSelectedRating] = useState("");

  const [users, setUsers] = useState({});
  const itemsPerPage = 4;

  const feedbackModalRef = useRef(null);
  const [currentFeedback, setCurrentFeedback] = useState(null);

  useEffect(() => {
    const fetchFeedbacks = async () => {
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/Feedback", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setFeedbackList(response.data);

        const uniqueUserIds = [...new Set(response.data.map((fb) => fb.userId))];
        await Promise.all(
          uniqueUserIds.map(async (userId) => {
            const userResponse = await axios.get(`http://localhost:5000/api/Users/${userId}`, {
              headers: { Authorization: `Bearer ${token}` },
            });
            setUsers((prevUsers) => ({
              ...prevUsers,
              [userId]: userResponse.data,
            }));
          })
        );
      } catch (error) {
        console.error("Error fetching data:", error);
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchFeedbacks();
  }, []);

  const totalPages = Math.ceil(feedbackList.length / itemsPerPage);
  // const currentFeedbacks = feedbackList.slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);

  // const openViewFeedbackModal = (feedback) => {
  //   setCurrentFeedback(feedback);
  //   feedbackModalRef.current.style.display = "block";
  //   document.querySelector(".modal-overlay").style.display = "block";
  // };

  const currentFeedbacks = feedbackList
    .filter((feedback) => {
      if (selectedMonth && new Date(feedback.submittedAt).getMonth() + 1 !== parseInt(selectedMonth)) return false;
      if (selectedRating && feedback.rating !== parseInt(selectedRating)) return false;
      return true;
    })
    .slice((currentPage - 1) * itemsPerPage, currentPage * itemsPerPage);

  const openViewFeedbackModal = (feedback) => {
    setCurrentFeedback(feedback);
    feedbackModalRef.current.style.display = "block";
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
    setSearchQuery(e.target.value);
  };

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
    if (feedbackList.length === 0) return 0;
    const totalRating = feedbackList.reduce((sum, feedback) => sum + feedback.rating, 0);
    return (totalRating / feedbackList.length).toFixed(1); // Làm tròn đến một chữ số thập phân
  };

  const averageRating = calculateAverageRating();

  return (
    <>
      <Header />
      {isDataLoading ? <Preloader /> : null}

      <div className="user-select">
        <h1>Quản lý phản hồi</h1>
        <h2>Phản hồi từ hệ thống SUPER GYM</h2>

        <div className="select-search-container">
          <div className="search-container">
            <input type="text" id="searchUser" className="form-control" placeholder="Tìm kiếm..." value={searchQuery} onChange={handleSearchChange} />
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
              <div className="col-sm-6">
                <button className="btn btn-success">
                  <AddCircleOutlineIcon /> Thêm mới
                </button>
              </div>
            </div>
          </div>
          <table className="table table-hover table-fixed">
            <thead>
              <tr>
                <th>User</th>
                <th>Message</th>
                <th>Rating</th>
                <th>Submitted At</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {currentFeedbacks.map((feedback) => {
                const user = users[feedback.userId];
                const rowClass = feedback.rating <= 3 ? "low-rating" : ""; // Apply class for low ratings
                return (
                  <tr key={feedback.feedbackId} className={rowClass}>
                    <td>
                      {user && (
                        <div className="user-info">
                          <img src={`data:image/jpeg;base64,${user.userAvatar}`} alt="User Avatar" className="customer-avatar" />
                          <span>{user.name}</span>
                        </div>
                      )}
                    </td>
                    <td>{feedback.message}</td>
                    <td>
                      {Array.from({ length: 5 }, (_, index) => (
                        <StarIcon
                          key={index}
                          style={{
                            color: index < feedback.rating ? "#FFD700" : "#E0E0E0", // Gold for rated stars, light gray for non-rated
                          }}
                        />
                      ))}
                    </td>
                    <td>{new Date(feedback.submittedAt).toLocaleDateString("vi-VN")}</td>
                    <td>
                      <a href="#" onClick={() => openViewFeedbackModal(feedback)} className="view">
                        <VisibilityIcon />
                      </a>
                      <a href="#" onClick={() => deleteFeedback(feedback.feedbackId)} className="delete">
                        <DeleteIcon />
                      </a>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>

          <div className="clearfix-el">
            <div className="hint-text">
              Showing <b>{currentFeedbacks.length}</b> out of <b>{feedbackList.length}</b> entries
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
                    <strong>User ID:</strong> {currentFeedback.userId}
                  </p>
                  <p>
                    <strong>Message:</strong> {currentFeedback.message}
                  </p>
                  <p>
                    <strong>Rating:</strong> {currentFeedback.rating}
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
