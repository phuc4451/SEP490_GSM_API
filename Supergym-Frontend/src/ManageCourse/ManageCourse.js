import React, { useEffect, useState } from "react";
import axios from "axios";
import Header from "../Header/Header";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageCourse.css";
import courseImg from "../assets/images/features-first-icon.png";
import LocalOfferIcon from "@mui/icons-material/LocalOffer";

const ManageCourse = () => {
  const [courses, setCourses] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [formData, setFormData] = useState({
    courseName: "",
    courseContent: "",
    courseDuration: "",
    coursePrice: "",
  });

  useEffect(() => {
    const fetchCourses = async () => {
      try {
        const token = localStorage.getItem("token");
        const response = await axios.get("http://localhost:5000/api/Course", {
          headers: { Authorization: `Bearer ${token}` },
        });
        setCourses(response.data);
      } catch (error) {
        console.error("Error fetching course data:", error);
      }
    };

    fetchCourses();
  }, []);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prevData) => ({ ...prevData, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const token = localStorage.getItem("token");
      const response = await axios.post("http://localhost:5000/api/Course", formData, { headers: { Authorization: `Bearer ${token}` } });
      setCourses((prevCourses) => [...prevCourses, response.data]);
      setShowForm(false);
      setFormData({
        courseName: "",
        courseContent: "",
        courseDuration: "",
        coursePrice: "",
      });
    } catch (error) {
      console.error("Error adding course:", error);
    }
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
                <button id="add-package-button" className="btn btn-primary" onClick={() => setShowForm(true)}>
                  Thêm gói +
                </button>
              </div>
            </div>
          </div>

          <div className="row">
            {courses.map((course) => (
              <div className="col-lg-6" key={course.courseId}>
                <ul className="features-items">
                  <li className="feature-item">
                    <div className="left-icon">
                      <img src={courseImg} alt="Course" />
                    </div>
                    <div className="right-content">
                      <div className="course-action">
                        <h4>{course.courseName}</h4>
                        <p>{course.courseContent}</p>
                        <a href="#" className="btn-fix detail-button">
                          Chi tiết
                        </a>
                        <a href="#" className="btn-fix edit-button">
                          Sửa
                        </a>
                        <a href="#" className="btn-fix delete-button">
                          Xóa
                        </a>
                      </div>

                      <div className="sale-price-section">
                        <button id="add-sale-button" className="btn btn-primary sale-btn">
                          Áp dụng giảm giá
                          <LocalOfferIcon />
                        </button>
                        <p className="price">Giá: {course.coursePrice.toLocaleString()} VND</p>
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
                  <h4 className="modal-title text-center mx-auto">Thêm gói tập</h4>
                  <button type="button" className="close" onClick={() => setShowForm(false)}>
                    &times;
                  </button>
                </div>
                <div className="modal-body">
                  <div className="form-group">
                    <label>Tên gói tập</label>
                    <input type="text" className="form-control" name="courseName" value={formData.courseName} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Nội dung</label>
                    <input type="text" className="form-control" name="courseContent" value={formData.courseContent} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Thời gian</label>
                    <input type="text" className="form-control" name="courseDuration" value={formData.courseDuration} onChange={handleInputChange} required />
                  </div>
                  <div className="form-group">
                    <label>Giá</label>
                    <input type="number" className="form-control" name="coursePrice" value={formData.coursePrice} onChange={handleInputChange} required />
                  </div>
                </div>
                <div className="modal-footer">
                  <button type="button" className="btn btn-default" onClick={() => setShowForm(false)}>
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
    </>
  );
};

export default ManageCourse;
