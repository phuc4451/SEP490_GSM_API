import React, { useState } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import "./Dashboard.css";
import Header from "../Header/Header.js";
import { Bar, Doughnut } from "react-chartjs-2";
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  BarElement,
  DoughnutController,
  ArcElement,
  Title,
  Tooltip,
  Legend,
} from "chart.js";

// Đăng ký các thành phần của Chart.js
ChartJS.register(CategoryScale, LinearScale, BarElement, DoughnutController, ArcElement, Title, Tooltip, Legend);

const Home = () => {
  // State cho các bộ lọc
  const [selectedMonth, setSelectedMonth] = useState("");
  const [selectedDay, setSelectedDay] = useState("");

  // Labels cho tháng và ngày trong tuần
  const monthlyLabels = ["Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"];
  const dailyLabels = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật"];

  // Dữ liệu gốc cho biểu đồ Tổng tiền theo tháng
  const originalMonthlyData = [1000000, 2000000, 1500000, 3000000, 2500000, 4000000, 4500000, 3800000, 3200000, 2900000, 3700000, 4200000];
  const originalDailyCheckinData = [150, 200, 180, 220, 210, 230, 170];

  // Dữ liệu đã lọc theo bộ lọc
  const filteredMonthlyData = {
    labels: selectedMonth ? [monthlyLabels[selectedMonth - 1]] : monthlyLabels,
    datasets: [
      {
        label: "Tổng tiền (VNĐ)",
        data: selectedMonth ? [originalMonthlyData[selectedMonth - 1]] : originalMonthlyData,
        backgroundColor: "rgba(54, 162, 235, 0.5)",
        borderColor: "rgba(54, 162, 235, 1)",
        borderWidth: 1,
      },
    ],
  };

  const filteredDailyCheckinData = {
    labels: selectedDay ? [dailyLabels[selectedDay - 1]] : dailyLabels,
    datasets: [
      {
        label: "Số lượt check-in",
        data: selectedDay ? [originalDailyCheckinData[selectedDay - 1]] : originalDailyCheckinData,
        backgroundColor: "rgba(75, 192, 192, 0.5)",
        borderColor: "rgba(75, 192, 192, 1)",
        borderWidth: 1,
      },
    ],
  };

  const packageSalesData = {
    labels: ["Gói A", "Gói B", "Gói C", "Gói D", "Gói E"],
    datasets: [
      {
        label: "Số lượng bán ra",
        data: [30, 20, 15, 25, 10],
        backgroundColor: ["rgba(255, 99, 132, 0.5)", "rgba(54, 162, 235, 0.5)", "rgba(255, 206, 86, 0.5)", "rgba(75, 192, 192, 0.5)", "rgba(153, 102, 255, 0.5)"],
        borderColor: ["rgba(255, 99, 132, 1)", "rgba(54, 162, 235, 1)", "rgba(255, 206, 86, 1)", "rgba(75, 192, 192, 1)", "rgba(153, 102, 255, 1)"],
        borderWidth: 1,
      },
    ],
  };

  // Cấu hình tùy chọn cho biểu đồ
  const options = {
    scales: {
      y: {
        beginAtZero: true,
        title: {
          display: true,
          text: "Số tiền (VNĐ)",
        },
      },
      x: {
        title: {
          display: true,
          text: "Tháng",
        },
      },
    },
    plugins: {
      legend: {
        display: true,
        position: "top",
      },
    },
  };

  const dailyCheckinOptions = {
    scales: {
      y: {
        beginAtZero: true,
        title: {
          display: true,
          text: "Số lượt check-in",
        },
      },
      x: {
        title: {
          display: true,
          text: "Ngày trong tuần",
        },
      },
    },
    plugins: {
      legend: {
        display: true,
        position: "top",
      },
    },
  };

  return (
    <>
      <Header />
      <section className="statistics-section">
        <div className="container">
          <div className="row">
            {/* <!-- Total Revenue --> */}
            <div className="col-lg-3 col-md-6">
              <div className="stat-card">
                <div className="stat-icon">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="40" height="40">
                    <rect width="256" height="256" fill="none"></rect>
                    <line x1="128" y1="24" x2="128" y2="232" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="16"></line>
                    <path d="M184,88a40,40,0,0,0-40-40H112a40,40,0,0,0,0,80h40a40,40,0,0,1,0,80H104a40,40,0,0,1-40-40" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="16"></path>
                  </svg>
                </div>
                <div className="stat-info">
                  <h4>Doanh thu trong tháng</h4>
                  <p>$14,321,145</p>
                  <span className="stat-percentage text-danger">↓ 4.5%</span>
                </div>
              </div>
            </div>

            {/* <!-- Pie Chart - Gói bán ra với thông tin bên phải --> */}
            <div className="col-lg-5 col-md-12 align-items-center stat-card">
              <div className="row w-100">
                <div className="col-md-6 d-flex align-items-center justify-content-center" style={{ maxWidth: "300px" }}>
                  <Doughnut data={packageSalesData} options={{ maintainAspectRatio: false }} />
                </div>

                <div className="col-md-6 d-flex flex-column justify-content-center stat-info">
                  <h4>Lượng gói bán ra</h4>
                  <ul>
                    <li>Gói A: 30</li>
                    <li>Gói B: 20</li>
                    <li>Gói C: 15</li>
                    <li>Gói D: 25</li>
                    <li>Gói E: 10</li>
                  </ul>
                </div>
              </div>
            </div>

            {/* <!-- Page Views --> */}
            <div className="col-lg-3 col-md-6">
              <div className="stat-card">
                <div className="stat-icon">
                  <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="40" height="40">
                    <rect width="256" height="256" fill="none"></rect>
                    <circle cx="84" cy="108" r="52" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="16"></circle>
                    <path d="M10.23,200a88,88,0,0,1,147.54,0" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="16"></path>
                    <path d="M172,160a87.93,87.93,0,0,1,73.77,40" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="16"></path>
                    <path d="M152.69,59.7A52,52,0,1,1,172,160" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round" strokeWidth="16"></path>
                  </svg>
                </div>
                <div className="stat-info">
                  <h4>Lượt khách đăng kí</h4>
                  <p>4,678</p>
                  <span className="stat-percentage text-success">↑ 15.5%</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Section chứa các biểu đồ chính */}
      <section className="chart-section">
        <div className="container">
          {/* Bộ lọc cho biểu đồ */}
          <div className="row mb-4">
            <div className="col-md-6">
              <label htmlFor="monthFilter">Chọn tháng:</label>
              <select
                id="monthFilter"
                className="form-select"
                value={selectedMonth}
                onChange={(e) => setSelectedMonth(e.target.value)}
              >
                <option value="">Tất cả tháng</option>
                {monthlyLabels.map((month, index) => (
                  <option key={index} value={index + 1}>{month}</option>
                ))}
              </select>
            </div>

            <div className="col-md-6">
              <label htmlFor="dayFilter">Chọn ngày trong tuần:</label>
              <select
                id="dayFilter"
                className="form-select"
                value={selectedDay}
                onChange={(e) => setSelectedDay(e.target.value)}
              >
                <option value="">Tất cả các ngày</option>
                {dailyLabels.map((day, index) => (
                  <option key={index} value={index + 1}>{day}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="row">
            {/* Biểu đồ Tổng tiền theo tháng */}
            <div className="col-md-6">
              <h4 className="chart-title">Biểu đồ Tổng tiền theo tháng</h4>
              <div className="small-chart">
                <Bar data={filteredMonthlyData} options={options} />
              </div>
            </div>

            {/* Biểu đồ Check-in theo ngày */}
            <div className="col-md-6">
              <h4 className="chart-title">Biểu đồ Check-in theo ngày</h4>
              <div className="small-chart">
                <Bar data={filteredDailyCheckinData} options={dailyCheckinOptions} />
              </div>
            </div>
          </div>
        </div>
      </section>
    </>
  );
};

export default Home;
