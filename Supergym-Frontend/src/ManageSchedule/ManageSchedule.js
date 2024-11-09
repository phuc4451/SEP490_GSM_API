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

  // Dữ liệu lịch tập
  const scheduleData = {
    monday: {
      fitnessClass: ["10:00AM - 11:30AM", "4:00PM - 5:30PM"],
      muscleTraining: ["", ""],
      bodyBuilding: ["2:00PM - 3:30PM", ""],
      yogaClass: ["", ""],
      advancedTraining: ["", ""],
    },
    tuesday: {
      fitnessClass: ["", "2:00PM - 3:30PM"],
      muscleTraining: ["", ""],
      bodyBuilding: ["10:00AM - 11:30AM", ""],
      yogaClass: ["", ""],
      advancedTraining: ["", ""],
    },
    wednesday: {
      fitnessClass: ["", ""],
      muscleTraining: ["", ""],
      bodyBuilding: ["", ""],
      yogaClass: ["10:00AM - 11:30AM", ""],
      advancedTraining: ["2:00PM - 3:30PM", ""],
    },
    thursday: {
      fitnessClass: ["", ""],
      muscleTraining: ["", "2:00PM - 3:30PM"],
      bodyBuilding: ["", ""],
      yogaClass: ["", ""],
      advancedTraining: ["10:00AM - 11:30AM", ""],
    },
    friday: {
      fitnessClass: ["", ""],
      muscleTraining: ["10:00AM - 11:30AM", ""],
      bodyBuilding: ["", ""],
      yogaClass: ["2:00PM - 3:30PM", ""],
      advancedTraining: ["", ""],
    },
  };

  // Hàm lấy lịch của từng loại lớp
  const getSchedule = (classType) => {
    return scheduleData[activeFilter][classType] || ["", ""];
  };

  // Hàm render thẻ select nếu có lịch
  const renderSelect = (timeSlot) => {
    return timeSlot ? (
      <div className="select-container">
        <select className="form-select">
          {personalTrainers.map((pt, index) => (
            <option key={index} value={pt}>
              {pt}
            </option>
          ))}
        </select>
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
                  Classes <em>Schedule</em>
                </h2>
                <img src="assets/images/line-dec.png" alt="" />
                <p>Nunc urna sem, laoreet ut metus id, aliquet consequat magna.</p>
              </div>
            </div>
          </div>
          <div className="row">
            <div className="col-lg-12">
              <div className="filters">
                <ul className="schedule-filter">
                  <li className={activeFilter === "monday" ? "active" : ""} onClick={() => handleFilterClick("monday")}>
                    Monday
                  </li>
                  <li className={activeFilter === "tuesday" ? "active" : ""} onClick={() => handleFilterClick("tuesday")}>
                    Tuesday
                  </li>
                  <li className={activeFilter === "wednesday" ? "active" : ""} onClick={() => handleFilterClick("wednesday")}>
                    Wednesday
                  </li>
                  <li className={activeFilter === "thursday" ? "active" : ""} onClick={() => handleFilterClick("thursday")}>
                    Thursday
                  </li>
                  <li className={activeFilter === "friday" ? "active" : ""} onClick={() => handleFilterClick("friday")}>
                    Friday
                  </li>
                </ul>
              </div>
            </div>
            <div className="col-lg-10 offset-lg-1">
              <div className="schedule-table filtering">
                <table>
                  <thead>
                    <tr>
                      <th></th> {/* Empty header cell */}
                      <th colSpan="2" style={{ textAlign: "center", color: "white" }}>
                        {/* Sáng */}
                      </th>
                      <th colSpan="2" style={{ textAlign: "center", color: "white" }}>
                        {/* Chiều */}
                      </th>
                    </tr>
                    <tr>
                      <th></th> {/* Empty header cell */}
                      <th style={{ textAlign: "center", color: "white" }}>Sáng</th>
                      <th style={{ textAlign: "center", color: "white" }}>Chiều</th>
                    </tr>
                  </thead>

                  <tbody>
                    <tr>
                      <td className="day-time">Fitness class</td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("fitnessClass")[0]}
                          {renderSelect(getSchedule("fitnessClass")[0])}
                        </div>
                      </td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("fitnessClass")[1]}
                          {renderSelect(getSchedule("fitnessClass")[1])}
                        </div>
                      </td>
                    </tr>
                    <tr>
                      <td className="day-time">Muscle Training</td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("muscleTraining")[0]}
                          {renderSelect(getSchedule("muscleTraining")[0])}
                        </div>
                      </td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("muscleTraining")[1]}
                          {renderSelect(getSchedule("muscleTraining")[1])}
                        </div>
                      </td>
                    </tr>
                    <tr>
                      <td className="day-time">Body Building</td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("bodyBuilding")[0]}
                          {renderSelect(getSchedule("bodyBuilding")[0])}
                        </div>
                      </td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("bodyBuilding")[1]}
                          {renderSelect(getSchedule("bodyBuilding")[1])}
                        </div>
                      </td>
                    </tr>
                    <tr>
                      <td className="day-time">Yoga Training</td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("yogaClass")[0]}
                          {renderSelect(getSchedule("yogaClass")[0])}
                        </div>
                      </td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("yogaClass")[1]}
                          {renderSelect(getSchedule("yogaClass")[1])}
                        </div>
                      </td>
                    </tr>
                    <tr>
                      <td className="day-time">Advanced Training</td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("advancedTraining")[0]}
                          {renderSelect(getSchedule("advancedTraining")[0])}
                        </div>
                      </td>
                      <td>
                        <div className="schedule-item">
                          {getSchedule("advancedTraining")[1]}
                          {renderSelect(getSchedule("advancedTraining")[1])}
                        </div>
                      </td>
                    </tr>
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
