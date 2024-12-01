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

const ManageMembership = () => {
  const [membershipDataList, setMembershipDataList] = useState([]); // renamed variable
  const [selectedOption, setSelectedOption] = useState(""); // State cho gói đã chọn
  const [selectedPackages, setSelectedPackages] = useState([]); // Dữ liệu các gói
  const [qrDataUrl, setQrDataUrl] = useState(""); // State to store the QR code data URL
  const showQrPicture = useRef(null);

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
  const itemsPerPage = 4; // Maximum 2 employees per page

  const membershipModalRef = useRef(null); // renamed ref
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);

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
      // renamed function
      const token = localStorage.getItem("token");
      try {
        const response = await axios.get("http://localhost:5000/api/GymMembership", {
          // updated API endpoint
          headers: { Authorization: `Bearer ${token}` },
        });
        setSelectedPackages(response.data); // updated variable
      } catch (error) {
        console.error("Error fetching memberships:", error); // renamed error message
      } finally {
        setIsDataLoading(false);
      }
    };
    fetchPackage(); // renamed function call
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
      gender: membership.user.gender.toLowerCase() === "nam" ? "male" : "female",
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

  const openViewDetail = (membership) => {
    // renamed function
    const dob = membership.user.dob || {}; // renamed variable
    const formattedDob = `${dob.year || "----"}-${String(dob.month || "01").padStart(2, "0")}-${String(dob.date || "01").padStart(2, "0")}`;

    setFormData({
      name: membership.user.name,
      gender: membership.user.gender.toLowerCase() === "nam" ? "male" : "female",
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

  const showSuccessModal = (message) => {
    setSuccessMessage(message); // Cập nhật thông báo
    successModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
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
  };

  const closeSuccessModal = () => {
    successModalRef.current.style.display = "none"; // Ẩn modal
    document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    window.location.reload();
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
      emails: emailInputs, // Emails from input fields
      boxingMembershipPlanId: null,
      gymMembershipId: packageInput,
      trainerRentalPlanId: null, // Ensure this is included
      qrPayment: qrPaymentInput,
      duration: 0, // Use the value of gymDuration
      selectedTimeSlot: "",
      isMonWedFri: true, // Only true for Gym
    };

    // Send the request based on the trainer type
    try {
      const token = localStorage.getItem("token");

      // If it's a Gym trainer
      const response = await fetch("http://localhost:5000/api/GymRegistration", {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json", // Specify content type as JSON
        },
        body: JSON.stringify(gymData), // Send gym data for gym trainer
      });

      const responsejson = await response.json();
      if (responsejson && responsejson[0] && responsejson[0].qrDataUrl) {
        setQrDataUrl(responsejson[0].qrDataUrl); // Set QR data URL in state
      }

      closeModal(); // Close the modal after success
      showQr(); // Show QR if applicable
    } catch (error) {
      console.error("Error during trainer registration:", error);
      showSuccessModal("Có lỗi khi đăng ký huấn luyện viên. Vui lòng thử lại.");
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

  return (
    <>
      <Header />

      {isLoading ? <Preloader /> : <div>{/* Nội dung khác của ManageMembership */}</div>}
      {/* <!-- ***** Preloader End ***** --> */}

      <div className="user-select">
        <h1>Quản lý người dùng đăng kí gói trong hệ thống super gym</h1>

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
                {/* <th className="action-el">Hành động</th> */}
              </tr>
            </thead>
            <tbody>
              {currentMemberships.map((membership, index) => (
                <tr key={index}>
                  <td>
                    <img src={`data:image/jpeg;base64,${membership.user.userAvatar}`} className="customer-avatar" />
                    {membership.user.name}
                  </td>
                  <td>{membership.user.gender === "Male" ? "Nam" : "Nữ"}</td>
                  <td>{formatDate(membership.user.dob)}</td>
                  <td>{membership.user.email}</td>
                  <td>{membership.user.phone}</td>
                  <td>{membership.user.address}</td>
                  {/* <td> */}
                    {/* <a href="#" onClick={() => openEditMembershipModal(membership)} className="edit">
                      <EditIcon />
                    </a>
                    <a href="#" onClick={() => openDeleteModal(membership)} className="delete">
                      <DeleteIcon />
                    </a> */}
                    {/* <a href="#" onClick={() => openAddMembershipModal(membership)} className="add">
                      <AddCircleOutlineIcon />
                    </a> */}
                    {/* <a href="#" onClick={() => openViewDetail(membership)} className="view">
                      <VisibilityIcon  />
                    </a> */}
                  {/* </td> */}
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
                    <label>Email</label>
                    <input type="email" className="form-control" name="email" value={formData.email} onChange={handleInputChange} required />
                    {errors.email && <div className="error-message">{errors.email}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Gói đăng kí</label>
                    <select className="form-control form-select" name="option" value={selectedOption} onChange={handlePackageChange} required>
                      <option value="">Chọn gói</option>
                      {selectedPackages.map((option, index) => (
                        <option key={index} value={option.gymMembershipId}>
                          {option.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Chọn kiểu thanh toán</label>
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
                <button type="button" className="btn btn-default" onClick={closeModal} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
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
    </>
  );
};

export default ManageMembership;
