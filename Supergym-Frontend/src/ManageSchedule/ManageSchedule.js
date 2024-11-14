import React, { useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageSchedule.css";
import Header from "../Header/Header.js";

const ManageSchedule = () => {
  const [activeFilter, setActiveFilter] = useState("monday");

  // Danh sách PT
  const personalTrainers = ["PT A", "PT B", "PT C", "PT D"];

  // Event handler for filter click
  const handleFilterClick = (filter) => {
    setActiveFilter(filter); // Cập nhật bộ lọc hiện tại
  };

  // Khung giờ từ 8h sáng đến 8h tối, mỗi khung giờ là 1 giờ
  const timeSlots = ["8:00AM", "9:00AM", "10:00AM", "11:00AM", "12:00PM", "1:00PM", "2:00PM", "3:00PM", "4:00PM", "5:00PM", "6:00PM", "7:00PM"];

  // Dữ liệu lịch tập
  const scheduleData = {
    monday: {
      PT1: Array(timeSlots.length).fill(""),
      PT2: Array(timeSlots.length).fill(""),
      PT3: Array(timeSlots.length).fill(""),
      PT4: Array(timeSlots.length).fill(""),
      PT5: Array(timeSlots.length).fill(""),
    },
    tuesday: {
      PT1: Array(timeSlots.length).fill(""),
      PT2: Array(timeSlots.length).fill(""),
      PT3: Array(timeSlots.length).fill(""),
      PT4: Array(timeSlots.length).fill(""),
      PT5: Array(timeSlots.length).fill(""),
    },
    wednesday: {
      PT1: Array(timeSlots.length).fill(""),
      PT2: Array(timeSlots.length).fill(""),
      PT3: Array(timeSlots.length).fill(""),
      PT4: Array(timeSlots.length).fill(""),
      PT5: Array(timeSlots.length).fill(""),
    },
    thursday: {
      PT1: Array(timeSlots.length).fill(""),
      PT2: Array(timeSlots.length).fill(""),
      PT3: Array(timeSlots.length).fill(""),
      PT4: Array(timeSlots.length).fill(""),
      PT5: Array(timeSlots.length).fill(""),
    },
    friday: {
      PT1: Array(timeSlots.length).fill(""),
      PT2: Array(timeSlots.length).fill(""),
      PT3: Array(timeSlots.length).fill(""),
      PT4: Array(timeSlots.length).fill(""),
      PT5: Array(timeSlots.length).fill(""),
    },
    saturday: {
      PT1: Array(timeSlots.length).fill(""),
      PT2: Array(timeSlots.length).fill(""),
      PT3: Array(timeSlots.length).fill(""),
      PT4: Array(timeSlots.length).fill(""),
      PT5: Array(timeSlots.length).fill(""),
    },
    sunday: {
      PT1: Array(timeSlots.length).fill(""),
      PT2: Array(timeSlots.length).fill(""),
      PT3: Array(timeSlots.length).fill(""),
      PT4: Array(timeSlots.length).fill(""),
      PT5: Array(timeSlots.length).fill(""),
    },
  };

  // Hàm ngẫu nhiên hiển thị nút "Chi tiết" hoặc "Thêm"
  const renderRandomButton = () => {
    const isDetailButton = Math.random() < 0.5; // 50% xác suất
    return isDetailButton ? <button className="btn btn-info btn-sm">Chi tiết</button> : <button className="btn btn-success btn-sm">Thêm</button>;
  };

  // Hàm lấy lịch của từng loại lớp
  const getSchedule = (classType) => {
    return scheduleData[activeFilter][classType] || Array(timeSlots.length).fill("");
  };

  // Hàm render thẻ select nếu có lịch
  const renderSelect = (timeSlot) => {
    return timeSlot ? (
      <div className="select-container">
        <div className="select-actions">
          <button className="btn btn-secondary btn-sm">Edit</button>
          <button className="btn btn-primary btn-sm">Save</button>
        </div>
      </div>
    ) : (
      ""
    );
  };
  return (
    <>
      <Header />

      {/* Phần SCHEDULE */}
      <section className="section" id="schedule">
        <div className="container">
          <div className="row">
            <div className="col-lg-6 offset-lg-3">
              <div className="section-heading-home dark-bg">
                <h2>
                  Quản lý <em>Lịch tập</em>
                </h2>
              </div>
            </div>
          </div>
          <div className="row">
            <div className="">
              <div className="filters">
                <ul className="schedule-filter">
                  <li className={activeFilter === "monday" ? "active" : ""} onClick={() => handleFilterClick("monday")}>
                    Thứ hai
                  </li>
                  <li className={activeFilter === "tuesday" ? "active" : ""} onClick={() => handleFilterClick("tuesday")}>
                    Thứ ba
                  </li>
                  <li className={activeFilter === "wednesday" ? "active" : ""} onClick={() => handleFilterClick("wednesday")}>
                    Thứ tư
                  </li>
                  <li className={activeFilter === "thursday" ? "active" : ""} onClick={() => handleFilterClick("thursday")}>
                    Thứ năm
                  </li>
                  <li className={activeFilter === "friday" ? "active" : ""} onClick={() => handleFilterClick("friday")}>
                    Thứ sáu
                  </li>
                  <li className={activeFilter === "saturday" ? "active" : ""} onClick={() => handleFilterClick("saturday")}>
                    Thứ bảy
                  </li>
                  <li className={activeFilter === "sunday" ? "active" : ""} onClick={() => handleFilterClick("sunday")}>
                    Chủ nhật
                  </li>
                </ul>
              </div>
            </div>
            <div className="">
              <div className="schedule-table filtering">
                <table>
                  <thead>
                    <tr>
                      <th></th> {/* Ô trống cho tên PT */}
                      {timeSlots.map((slot, index) => (
                        <th key={index} style={{ textAlign: "center", color: "white" }}>
                          {slot}
                        </th>
                      ))}
                    </tr>
                  </thead>

                  <tbody>
                    {["PT1", "PT2", "PT3", "PT4", "PT5"].map((pt, index) => (
                      <tr key={index}>
                        <td className="day-time">{`PT ${index + 1}`}</td>
                        {getSchedule(pt).map((_, idx) => (
                          <td key={idx}>
                            <div className="schedule-item">{renderRandomButton()}</div>
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </section>
    </>
  );
};

export default ManageSchedule;
