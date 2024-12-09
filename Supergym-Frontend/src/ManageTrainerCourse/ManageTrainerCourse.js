import React, { useState, useEffect, useRef } from "react";

import axios from "axios";
import Header from "../Header/Header";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageTrainerCourse.css";
import courseImg from "../assets/images/features-first-icon.png";
import LocalOfferIcon from "@mui/icons-material/LocalOffer";
import CloseIcon from "@mui/icons-material/Close";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
const ManageTrainerCourse = () => {
  const fileInputRef = useRef(null);

  const [successMessage, setSuccessMessage] = useState("");
  const [customerToDelete, setCustomerToDelete] = useState(null); // for deletion
  const [currentCustomer, setCurrentCustomer] = useState(null); // for editing customer
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const [errors, setErrors] = useState({});
  const [customerData, setCustomerData] = useState([]);
  const [rentalOptions, setRentalOptions] = useState([]); // Renamed to reflect rental options data
  const [errorMessage, setErrorMessage] = useState("");
  const errorModalRef = useRef(null);

  const [courses, setCourses] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState({
    description: "",
    sessionCountMin: "",
    sessionCountMax: "",
    memberCount: "",
    pricePerPersonPerSession: "",
    pricePerPersonPerMonth: "",
  });

  const openAddCustomerModal = () => {
    setFormData({
      description: "",
      sessionCountMin: "",
      sessionCountMax: "",
      memberCount: "",
      pricePerPersonPerSession: "",
      pricePerPersonPerMonth: "",
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

  const openEditCustomerModal = (course) => {
    setFormData({
      description: course.description || "", // Add default values in case of missing data
      sessionCountMin: course.sessionCountMin || "",
      sessionCountMax: course.sessionCountMax || "",
      memberCount: course.memberCount || "",
      pricePerPersonPerSession: course.pricePerPersonPerSession || "",
      pricePerPersonPerMonth: course.pricePerPersonPerMonth || "",
    });
    setCurrentCustomer(course); // Set current customer for editing
    customerModalRef.current.style.display = "block"; // Show modal
    customerModalRef.current.classList.add("active"); // Add active class for styling
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeModal = () => {
    customerModalRef.current.style.display = "none";
    customerModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentCustomer(null); // Đặt lại khách hàng hiện tại
    setPreviewImage(null); // Đặt lại ảnh xem trước
    setFormData({
      description: "",
      sessionCountMin: "",
      sessionCountMax: "",
      memberCount: "",
      pricePerPersonPerSession: "",
      pricePerPersonPerMonth: "",
    });
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

  const customerModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);

  useEffect(() => {
    const fetchCourses = async () => {
      try {
        const token = localStorage.getItem("token"); // Get token from localStorage
        const response = await axios.get("http://localhost:5000/api/RentalOption", {
          // Correct API URL
          headers: {
            Authorization: `Bearer ${token}`, // Ensure Authorization header is correctly formatted
          },
        });
        setCourses(response.data); // Update state with the fetched data
      } catch (error) {
        console.error("Error fetching course data:", error); // Handle error if the fetch fails
      }
    };

    fetchCourses();
  }, []); // Empty dependency array ensures this only runs once on component mount

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prevData) => ({
      ...prevData,
      [name]: value, // Cập nhật giá trị tương ứng trong formData
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const token = localStorage.getItem("token");
      const response = await axios.post("http://localhost:5000/api/RentalOption", formData, {
        headers: { Authorization: `Bearer ${token}` },
      });
      setCourses((prevCourses) => [...prevCourses, response.data]);
      setShowForm(false);
      setFormData({
        description: "string",
        sessionCountMin: 0,
        sessionCountMax: 0,
        memberCount: 0,
        pricePerPersonPerSession: 0,
        pricePerPersonPerMonth: 0,
      });
      closeModal();
      showSuccessModal("Gói Boxing được lưu thành công");
    } catch (error) {
      if (error.response && error.response.status === 409) {
        showErrorModal("Gói Trainer đã tồn tại, vui lòng thêm gói Trainer khác!");
      } else {
        showErrorModal("Lỗi khi tạo gói Trainer");
        console.error("Error saving equipment:", error);
      }
    }
  };

  const showErrorModal = (errMessage) => {
    setErrorMessage(errMessage);
    errorModalRef.current.style.display = "block";
    document.querySelector(".modal-overlay").style.display = "block";
  };

  const closeErrorModal = () => {
    errorModalRef.current.style.display = "none"; // Ẩn modal
    // document.querySelector(".modal-overlay").style.display = "none"; // Ẩn overlay
    // window.location.reload();
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
  const openDeleteModal = (customer) => {
    setCustomerToDelete(customer); // Set customer to be deleted
    deleteModalRef.current.style.display = "block"; // Show delete modal
    document.querySelector(".modal-overlay").style.display = "block"; // Show overlay
  };

  const closeDeleteModal = () => {
    deleteModalRef.current.style.display = "none"; // Hide delete modal
    document.querySelector(".modal-overlay").style.display = "none"; // Hide overlay
  };

  return (
    <>
      <Header />

      <section className="section" id="features">
        <div className="container">
          <div className="row">
            <div className="col-lg-6 offset-lg-3">
              <div className="section-heading">
                <h2>Danh sách các gói tập</h2>
                <button onClick={openAddCustomerModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm gói Trainer</span>
                </button>
              </div>
            </div>
          </div>

          <div className="row">
            {courses.map((course) => (
              <div className="col-lg-6" key={course.gymMembershipId}>
                <ul className="features-items">
                  <li className="feature-item">
                    <div className="left-icon">
                      <img src={courseImg} alt="Course" />
                    </div>
                    <div className="right-content">
                      <div className="course-action">
                        <h4>{course.description}</h4>

                        <div className="row">
                          <div className="col-lg-6">
                            <p>Số buổi tối thiểu: {course.sessionCountMin}</p>
                            <p>Số buổi tối đa: {course.sessionCountMax}</p>
                          </div>
                          <div className="col-lg-6">
                            <p>Thành viên trong gói: {course.memberCount}</p>
                          </div>
                        </div>

                        <a href="#" onClick={() => openEditCustomerModal(course)} className="btn-fix detail-button">
                          Chi tiết
                        </a>
                        {/* <a href="#" className="btn-fix edit-button">
                          Sửa
                        </a>
                        <a href="#" className="btn-fix delete-button">
                          Xóa
                        </a> */}
                      </div>

                      <div className="sale-price-section">
                        {/* <button id="add-sale-button" className="btn btn-primary sale-btn">
                          Áp dụng giảm giá
                          <LocalOfferIcon />
                        </button> */}
                        {course.pricePerPersonPerSession > 0 && <p className="price">Giá 1 người/buổi: {course.pricePerPersonPerSession} VND</p>}
                        {course.pricePerPersonPerMonth > 0 && <p className="price">Giá 1 người/tháng: {course.pricePerPersonPerMonth} VND</p>}
                      </div>
                    </div>
                  </li>
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      {showForm && (
        <div className="modal">
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
              <form onSubmit={handleSubmit}>
                <div className="modal-header">
                  <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Sửa gói tập" : "Thêm gói tập"}</h4>
                  <button type="button" className="close" onClick={() => setShowForm(false)}>
                    &times;
                  </button>
                </div>
                <div className="modal-body">
                  <div className="form-group">
                    <label>Tên gói tập</label>
                    <input type="text" className="form-control" name="name" value={formData.name} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Thời gian (tháng)</label>
                    <input type="number" className="form-control" name="durationMonths" value={formData.durationMonths} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Số buổi</label>
                    <input type="number" className="form-control" name="sessionCount" value={formData.sessionCount} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Giá</label>
                    <input type="number" className="form-control" name="price" value={formData.price} onChange={handleInputChange} required />
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-default" onClick={() => setShowForm(false)} style={{ backgroundColor: "white", color: "black", borderColor: "lightgray" }}>
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
      )}
      {showForm && <div className="modal-overlay" onClick={() => setShowForm(false)}></div>}

      {/* customer Modal */}
      <div ref={customerModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="employeeForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Chi tiết gói tập Trainer" : "Thêm gói Trainer"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* Input Fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên gói tập <span className="icon-input">(*)</span>
                    </label>
                    <input
                      type="text"
                      className="form-control"
                      name="description" // binding với 'description' trong formData
                      value={formData.description} // binding với formData
                      onChange={handleInputChange} // Hàm thay đổi
                      required
                    />
                  </div>

                  <div className="form-group col">
                    <label>
                      Số buổi tối thiểu <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className="form-control" name="sessionCountMin" value={formData.sessionCountMin} onChange={handleInputChange} required />
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Số buổi tối đa <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className="form-control" name="sessionCountMax" value={formData.sessionCountMax} onChange={handleInputChange} required />
                  </div>

                  <div className="form-group col">
                    <label>
                      Thành viên trong gói <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className="form-control" name="memberCount" value={formData.memberCount} onChange={handleInputChange} required />
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Giá 1 người/buổi <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className="form-control" name="pricePerPersonPerSession" value={formData.pricePerPersonPerSession} onChange={handleInputChange} required />
                  </div>

                  <div className="form-group col">
                    <label>
                      Giá 1 người/tháng <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className="form-control" name="pricePerPersonPerMonth" value={formData.pricePerPersonPerMonth} onChange={handleInputChange} required />
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
              <p>{errorMessage}</p>
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

export default ManageTrainerCourse;
