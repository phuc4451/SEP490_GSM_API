import React from "react";
import Header from "../Header/Header";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageSale.css";
import courseImg from "../assets/images/features-first-icon.png";


const ManageSale = () => {
  return (
    <>
      <Header />

      <section className="section" id="features">
      <div className="container">
        <div className="row">
          <div className="col-lg-6 offset-lg-3">
            <div className="section-heading">
              <h2>Danh sách gói khuyến mãi</h2>
              <button id="add-package-button" className="btn btn-primary">
                Thêm gói +
              </button>
            </div>
          </div>
          <div className="col-lg-6">
            <ul className="features-items">
              <li className="feature-item">
                <div className="left-icon">
                  <img src={courseImg} alt="First One" />
                </div>
                <div className="right-content">
                  <div className="course-action">
                    <h4>Gói dành cho sinh viên</h4>
                    <p>Giá 20% cho toàn bộ sinh viên khi đăng ký tập</p>
                    <a href="#" className="btn-fix detail-button">Chi tiết</a>
                    <a href="#" className="btn-fix edit-button">Sửa</a>
                    <a href="#" className="btn-fix delete-button">Xóa</a>
                  </div>
                  <div className="sale-price-section">
                    <p className="sale">Giảm: 20%</p>
                  </div>
                </div>
              </li>
              <li className="feature-item">
                <div className="left-icon">
                  <img
                    src={courseImg}
                    alt="second one"
                  />
                </div>
                <div className="right-content">
                  <div className="course-action">
                    <h4>Gói người mới</h4>
                    <p>Đăng kí tập lần đầu được giảm 25% mọi cơ sở</p>
                    <a href="#" className="btn-fix detail-button">Chi tiết</a>
                    <a href="#" className="btn-fix edit-button">Sửa</a>
                    <a href="#" className="btn-fix delete-button">Xóa</a>
                  </div>
                  <div className="sale-price-section">
                    <p className="sale">Giảm 25%</p>
                  </div>
                </div>
              </li>
            </ul>
          </div>
          <div className="col-lg-6">
            <ul className="features-items">
              <li className="feature-item">
                <div className="left-icon">
                  <img
                    src={courseImg}
                    alt="fourth muscle"
                  />
                </div>
                <div className="right-content">
                  <div className="course-action">
                    <h4>Gói thuê PT lần đầu</h4>
                    <p>tiền phòng giảm 100% đối với người thuê PT lần đầu</p>
                    <a href="#" className="btn-fix detail-button">Chi tiết</a>
                    <a href="#" className="btn-fix edit-button">Sửa</a>
                    <a href="#" className="btn-fix delete-button">Xóa</a>
                  </div>
                  <div className="sale-price-section">
                    <p className="sale">Giảm tiền phòng</p>
                  </div>
                </div>
              </li>
              <li className="feature-item">
                <div className="left-icon">
                  <img
                    src={courseImg}
                    alt="training fifth"
                  />
                </div>
                <div className="right-content">
                  <div className="course-action">
                    <h4>Gói BOXING + YOGA</h4>
                    <p>Giảm 20% đối với khách hàng khi đăng kí tập cả 2 lớp</p>
                    <a href="#" className="btn-fix detail-button">Chi tiết</a>
                    <a href="#" className="btn-fix edit-button">Sửa</a>
                    <a href="#" className="btn-fix delete-button">Xóa</a>
                  </div>
                  <div className="sale-price-section">
                    <p className="sale">Giảm 20%</p>
                  </div>
                </div>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </section>
    </>
  );
};

export default ManageSale;
