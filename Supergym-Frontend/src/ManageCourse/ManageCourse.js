import React, { useState, useEffect, useRef } from "react";

import axios from "axios";
import Header from "../Header/Header";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageCourse.css";
import courseImg from "../assets/images/features-first-icon.png";
import LocalOfferIcon from "@mui/icons-material/LocalOffer";
import CloseIcon from "@mui/icons-material/Close";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import Preloader from "../Preloader/Preloader";

const ManageCourse = () => {
  const fileInputRef = useRef(null);

  const [successMessage, setSuccessMessage] = useState("");
  const [customerToDelete, setCustomerToDelete] = useState(null); // for deletion
  const [currentCustomer, setCurrentCustomer] = useState(null); // for editing customer
  const [previewImage, setPreviewImage] = useState(null); // To store preview image
  const [errors, setErrors] = useState({});
  const [customerData, setCustomerData] = useState([]);
  const errorModalRef = useRef(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [isDataLoading, setIsDataLoading] = useState(true);

  const [courses, setCourses] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState({
    name: "",
    durationMonths: "",
    sessionCount: "",
    price: "",
  });

  const formatNumberWithCommas = (number) => {
    return number.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  };
  
  // Helper function to remove commas from string
  const removeCommas = (str) => {
    return str.replace(/,/g, "");
  };

  const openAddCustomerModal = () => {
    setFormData({
      name: "",
      durationMonths: "",
      sessionCount: "",
      price: "",
    });
    setErrors({}); // Xóa lỗi trước đó
    setErrorMessage(""); // Xóa thông báo lỗi trước đó
    setCurrentCustomer(null); // Đặt lại khách hàng hiện tại để thêm mới
    customerModalRef.current.style.display = "block"; // Hiển thị modal
    customerModalRef.current.classList.add("active"); // Thêm class 'active'
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openEditCustomerModal = (course) => {
    setFormData({
      name: course.name,
      durationMonths: course.durationMonths,
      sessionCount: course.sessionCount,
      price: course.price.toLocaleString(),
    });
    setErrors({}); // Xóa lỗi trước đó
    setErrorMessage(""); // Xóa thông báo lỗi trước đó
    setCurrentCustomer(course); // Lưu thông tin gói tập để biết được ID của gói cần chỉnh sửa
    customerModalRef.current.style.display = "block"; // Hiển thị modal
    customerModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const openDetailModal = (course) => {
    setFormData({
      name: course.name,
      durationMonths: course.durationMonths,
      sessionCount: course.sessionCount,
      price: course.price.toLocaleString(),
    });
    setErrors({}); // Xóa lỗi trước đó
    setErrorMessage(""); // Xóa thông báo lỗi trước đó
    setCurrentCustomer(course); // Lưu thông tin gói tập để biết được ID của gói cần chỉnh sửa
    detailModalRef.current.style.display = "block"; // Hiển thị modal
    detailModalRef.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block"; // Hiển thị overlay
  };

  const closeModal = () => {
    detailModalRef.current.style.display = "none";
    detailModalRef.current.classList.remove("active");
    customerModalRef.current.style.display = "none";
    customerModalRef.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentCustomer(null); // Đặt lại khách hàng hiện tại
    setPreviewImage(null); // Đặt lại ảnh xem trước
    setFormData({
      name: "",
      durationMonths: "",
      sessionCount: "",
      price: "",
    });
  };

  const handleImageChange = (e) => {
    const file = e.target.files[0];
    if (file) {
      setFormData({
        ...formData,
        userAvatar: file,
      });

      const reader = new FileReader();
      reader.onloadend = () => {
        setPreviewImage(reader.result);
      };
      reader.readAsDataURL(file);
    }
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
  const detailModalRef = useRef(null);
  const successModalRef = useRef(null);
  const deleteModalRef = useRef(null);

  useEffect(() => {
    const fetchCourses = async () => {
      try {
        const token = localStorage.getItem("token");
        const response = await axios.get("http://localhost:5000/api/GymMembership", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setCourses(response.data);
      } catch (error) {
        console.error("Error fetching course data:", error);
      } finally {
        setIsDataLoading(false);
      }
    };

    fetchCourses();
  }, []);


  const handleInputChange = (e) => {
    const { name, value } = e.target;
  
    if (name === "price") {
      // Allow empty value for price
      if (value === '') {
        setFormData({
          ...formData,
          [name]: ''
        });
        setErrors((prevErrors) => ({
          ...prevErrors,
          price: "Giá phải là số dương."
        }));
        return;
      }
  
      // Remove existing commas and convert to number
      const numericValue = removeCommas(value);
  
      // Check if it's a valid number
      if (!isNaN(numericValue)) {
        const numberValue = parseFloat(numericValue);
        // Only format if it's a positive number
        if (numberValue >= 0) {
          setFormData({
            ...formData,
            [name]: formatNumberWithCommas(numericValue)
          });
          // Clear error if valid
          setErrors((prevErrors) => {
            const newErrors = { ...prevErrors };
            delete newErrors.price;
            return newErrors;
          });
        } else {
          setErrors((prevErrors) => ({
            ...prevErrors,
            price: "Giá phải là số dương."
          }));
        }
      } else {
        setErrors((prevErrors) => ({
          ...prevErrors,
          price: "Giá phải là số dương."
        }));
      }
    } else if (name === "durationMonths") {
      // Handle duration input
      if (value === '') {
        setFormData({
          ...formData,
          [name]: ''
        });
        setErrors((prevErrors) => ({
          ...prevErrors,
          durationMonths: "Thời gian phải lớn hơn hoặc bằng 0."
        }));
        return;
      }
  
      const numberValue = Number(value);
      if (!isNaN(numberValue) && numberValue >= 0) {
        setFormData({
          ...formData,
          [name]: value
        });
        // Clear error if valid
        setErrors((prevErrors) => {
          const newErrors = { ...prevErrors };
          delete newErrors.durationMonths;
          return newErrors;
        });
      } else {
        setErrors((prevErrors) => ({
          ...prevErrors,
          durationMonths: "Thời gian phải lớn hơn hoặc bằng 0."
        }));
      }
    } else {
      setFormData({
        ...formData,
        [name]: value
      });
      validateField(name, value);
    }
  };


const validateField = (name, value) => {
  const newErrors = { ...errors };

  switch (name) {
    case "name":
      if (value.length <= 0) {
        newErrors.name = "Tên gói phải có ít nhất từ 1 ký tự.";
      } else {
        delete newErrors.name;
      }
      break;
    case "durationMonths":
      if (value.length <= 0) {
        newErrors.durationMonths = "Thời gian phải lớn hơn 0";
      } else {
        delete newErrors.durationMonths;
      }
      break;
    case "price":
      if (value === '' || value <= 0) {
        newErrors.price = "Giá gói tập phải lớn hơn 0";
      } else {
        delete newErrors.price;
      }
      break;
    case "sessionCount":
      if (!value) {
        newErrors.sessionCount = "Số buổi phải lớn hơn hoặc bằng 0";
      } else {
        delete newErrors.sessionCount;
      }
      break;
    default:
      break;
  }

  setErrors(newErrors);
};

  const validateForm = () => {
    const newErrors = {};

    // Check all fields here
    if (formData.name.length <= 0) {
      newErrors.name = "Tên gói phải có ít nhất 1 ký tự.";
    }

    if (formData.durationMonths.length < 0) {
      newErrors.name = "Thời gian phải lớn hoặc bằng hơn 0";
    }

    if (formData.price.length <= 0) {
      newErrors.name = "Giá gói tập phải lớn hơn 0";
    }

    if (formData.sessionCount.length < 0) {
      newErrors.name = "Số buổi phải lớn hơn hoặc bằng 0";
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0; // If no errors, form is valid
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (validateForm()) {
      try {
        const token = localStorage.getItem("token");
        // Create a new object with the price converted to number
        const submitData = {
          ...formData,
          price: parseFloat(removeCommas(formData.price)),
          sessionCount: 0
        };

        const response = await axios.post(
          "http://localhost:5000/api/GymMembership",
          submitData,
          {
            headers: { Authorization: `Bearer ${token}` },
          }
        );
        setCourses((prevCourses) => [...prevCourses, response.data]);
        setFormData({
          name: "",
          durationMonths: "",
          sessionCount: "",
          price: "",
        });
        closeModal();
        showSuccessModal("Gói Gym được lưu thành công");
      } catch (error) {
        if (error.response && error.response.status === 409) {
          showErrorModal("Gói Gym đã tồn tại, vui lòng thêm gói Gym khác!");
        } else {
          showErrorModal("Lỗi khi tạo gói Gym");
          console.error("Error saving equipment:", error);
        }
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
      {isDataLoading ? <Preloader /> : null}
      <section className="section" id="features">
        <div className="container">
          <div className="row">
            <div className="col-lg-6 offset-lg-3">
              <div className="section-heading">
                <h2>Danh sách các gói tập</h2>
                <button onClick={openAddCustomerModal} className="btn btn-success">
                  <AddCircleOutlineIcon />
                  <span>Thêm gói Gym</span>
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
                        <h4>{course.name}</h4>
                        <div className="row">
                          <div className="col-lg-6">
                            <p>Thời gian: {course.durationMonths} tháng</p>
                            <p>Số buổi: {course.sessionCount}</p>
                          </div>
                        </div>

                        <a href="#" onClick={() => openDetailModal(course)} className="btn-fix detail-button">
                          Chi tiết
                        </a>
                        {/* <a href="#" className="btn-fix edit-button">
                          Sửa
                        </a>
                        <a href="#" className="btn-fix delete-button">
                          Xóa
                        </a> */}
                        <p className="price">Giá gói: {course.price.toLocaleString()} VND</p>
                      </div>

                      <div className="sale-price-section">
                        {/* <button id="add-sale-button" className="btn btn-primary sale-btn">
                          Áp dụng giảm giá
                          <LocalOfferIcon />
                        </button> */}
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
                    <input type="text" className={`form-control ${errors.name ? "is-invalid" : ""}`} name="name" value={formData.name} onChange={handleInputChange} required />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>
                  <div className="form-group">
                    <label>Thời gian (tháng)</label>
                    <input type="number" className={`form-control ${errors.durationMonths ? "is-invalid" : ""}`} name="durationMonths" value={formData.durationMonths} onChange={handleInputChange} required />
                    {errors.durationMonths && <div className="error-message">{errors.durationMonths}</div>}
                  </div>
                  <div className="form-group">
                    <label>Số buổi</label>
                    <input type="number" className={`form-control ${errors.sessionCount ? "is-invalid" : ""}`} name="sessionCount" value={formData.sessionCount} onChange={handleInputChange} required />
                    {errors.sessionCount && <div className="error-message">{errors.sessionCount}</div>}
                  </div>
                  <div className="form-group">
                    <label>Giá</label>
                    <input type="number" className={`form-control ${errors.price ? "is-invalid" : ""}`} name="price" value={formData.price} onChange={handleInputChange} required />
                    {errors.price && <div className="error-message">{errors.price}</div>}
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
                <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Chi tiết gói tập gym" : "Thêm gói Gym"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên gói tập <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.name ? "is-invalid" : ""}`} name="name" value={formData.name} onChange={handleInputChange} required />
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Thời gian <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${errors.durationMonths ? "is-invalid" : ""}`} name="durationMonths" value={formData.durationMonths} onChange={handleInputChange} required />
                    {errors.durationMonths && <div className="error-message">{errors.durationMonths}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Giá <span className="icon-input">(*)</span>
                    </label>
                    <div className="input-group">
                      <input type="text" className={`form-control ${errors.price ? "is-invalid" : ""}`} name="price" value={formData.price} onChange={handleInputChange} required />

                      <span className="input-readonly">VNĐ</span>
                    </div>

                    {errors.price && <div className="error-message">{errors.price}</div>}
                  </div>

                  {/* <div className="form-group col">
                    <label>
                      Số buổi <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${errors.sessionCount ? "is-invalid" : ""}`} name="sessionCount" value={formData.sessionCount} onChange={handleInputChange} required />
                    {errors.sessionCount && <div className="error-message">{errors.sessionCount}</div>}
                  </div> */}
                </div>

                <div className="row"></div>

                {/* Conditionally render the password input only if adding a new customer */}
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


      <div ref={detailModalRef} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="employeeForm" onSubmit={handleSubmit}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">{currentCustomer ? "Chi tiết gói tập gym" : "Thêm gói Gym"}</h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>
              <div className="modal-body">
                {/* First row of input fields */}
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên gói tập <span className="icon-input">(*)</span>
                    </label>
                    <input type="text" className={`form-control ${errors.name ? "is-invalid" : ""}`} name="name" value={formData.name} onChange={handleInputChange} required disabled/>
                    {errors.name && <div className="error-message">{errors.name}</div>}
                  </div>

                  <div className="form-group col">
                    <label>
                      Số tháng <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${errors.durationMonths ? "is-invalid" : ""}`} name="durationMonths" value={formData.durationMonths} onChange={handleInputChange} required disabled/>
                    {errors.durationMonths && <div className="error-message">{errors.durationMonths}</div>}
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>
                      Giá <span className="icon-input">(*)</span>
                    </label>
                    <div className="input-group">
                      <input type="text" className={`form-control ${errors.price ? "is-invalid" : ""}`} name="price" value={formData.price} onChange={handleInputChange} required disabled/>

                      <span className="input-readonly">VNĐ</span>
                    </div>

                    {errors.price && <div className="error-message">{errors.price}</div>}
                  </div>

                  {/* <div className="form-group col">
                    <label>
                      Số buổi <span className="icon-input">(*)</span>
                    </label>
                    <input type="number" className={`form-control ${errors.sessionCount ? "is-invalid" : ""}`} name="sessionCount" value={formData.sessionCount} onChange={handleInputChange} required disabled/>
                    {errors.sessionCount && <div className="error-message">{errors.sessionCount}</div>}
                  </div> */}
                </div>

                <div className="row"></div>

                {/* Conditionally render the password input only if adding a new customer */}
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

export default ManageCourse;
