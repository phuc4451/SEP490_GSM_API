import React, { useState, useEffect } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import "./Dashboard.css";
import Header from "../Header/Header.js";
import { Bar, Doughnut } from "react-chartjs-2";
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, DoughnutController, ArcElement, Title, Tooltip, Legend } from "chart.js";

// Đăng ký các thành phần của Chart.js
ChartJS.register(CategoryScale, LinearScale, BarElement, DoughnutController, ArcElement, Title, Tooltip, Legend);

const Home = () => {
  // State cho các bộ lọc
  const [selectedMonth, setSelectedMonth] = useState("");
  const [selectedDay, setSelectedDay] = useState("");
  const [monthlyData, setMonthlyData] = useState([]);
  const [totalRevenueAllMonth, setTotalRevenueAllMonth] = useState([]);
  const [dailyCheckinData, setDailyCheckinData] = useState([]);
  // Labels cho tháng và ngày trong tuần
  const monthlyLabels = ["Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"];
  const dailyLabels = ["Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật"];

  // Dữ liệu gốc cho biểu đồ Tổng tiền theo tháng
  const originalMonthlyData = [1000000, 2000000, 1500000, 3000000, 2500000, 4000000, 4500000, 3800000, 3200000, 2900000, 3700000, 4200000];
  const originalDailyCheckinData = [150, 200, 180, 220, 210, 230, 170];

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

  // Dữ liệu từ API
  const [revenueData, setRevenueData] = useState({
    revenueThisMonth: 0,
    revenueLastMonth: 0,
    totalRevenue: 0,
    growthPercentage: 0,
  });
  const [registrationData, setRegistrationData] = useState({
    registrationsThisMonth: 0,
    registrationsLastMonth: 0,
    totalRegistrations: 0,
    growthPercentage: 0,
  });
  const [packageSold, setPackageSold] = useState({
    totalPackage: 0,
    namePackage: [],
    quantityPackage: [],
  });

  useEffect(() => {
    // Lấy dữ liệu doanh thu từ API
    const fetchRevenueData = async () => {
      try {
        const response = await fetch("http://localhost:5000/api/Finance/revenue-growth");
        const data = await response.json();
        setRevenueData(data);
      } catch (error) {
        console.error("Error fetching revenue data:", error);
      }
    };

    // Lấy dữ liệu lượt khách đăng ký từ API
    const fetchRegistrationData = async () => {
      try {
        const response = await fetch("http://localhost:5000/api/Finance/registration-growth");
        const data = await response.json();
        setRegistrationData({
          registrationsThisMonth: data.registrationsThisMonth,
          registrationsLastMonth: data.registrationsLastMonth,
          totalRegistrations: data.totalRegistrations,
          growthPercentage: data.growthPercentage,
        });
      } catch (error) {
        console.error("Error fetching registration data:", error);
      }
    };

    const fetchPackageSold = async () => {
      try {
        // Get current year
        const currentYear = new Date().getFullYear();

        // Calculate the start and end dates of the year
        const startDate = new Date(currentYear, 0, 1); // January 1st of current year
        const endDate = new Date(currentYear, 11, 31); // December 31st of current year

        // Format dates to YYYY-MM-DD (use ISO string and slice to get the date part)
        const startDateString = startDate.toISOString().slice(0, 10);
        const endDateString = endDate.toISOString().slice(0, 10);

        // Call the API with dynamic start and end dates
        const response = await fetch(`http://localhost:5000/api/Finance/sold-packages?startDate=${startDateString}&endDate=${endDateString}`);
        const data = await response.json();

        // Calculate the total packages and extract names and quantities
        let totalPackage = 0;
        const namePackage = [];
        const quantityPackage = [];

        // Iterate through the data to extract package names and quantities
        for (const packageName in data) {
          if (data.hasOwnProperty(packageName)) {
            namePackage.push(packageName);
            quantityPackage.push(data[packageName]);
            totalPackage += data[packageName]; // Add the quantity to the total package count
          }
        }

        // Update the state with the calculated values
        setPackageSold({
          totalPackage,
          namePackage,
          quantityPackage,
        });
      } catch (error) {
        console.error("Error fetching package sold data:", error);
      }
    };

    const fetchMonthlyRevenueData = async () => {
      try {
        const response = await fetch("http://localhost:5000/api/Finance/revenue?month=TotalRevenue");
        const data = await response.json();

        if (data && data.monthlyRevenue) {
          setTotalRevenueAllMonth(data.totalRevenue);
          const revenues = data.monthlyRevenue.map((item) => item.revenue); // Extract revenue values
          setMonthlyData(revenues); // Store the revenue data in state
        }
      } catch (error) {
        console.error("Error fetching monthly revenue data:", error);
      }
    };

    const fetchCheckinData = async () => {
      try {
        const response = await fetch("http://localhost:5000/api/Finance/checkin?day=AllDays");
        const data = await response.json();

        // Lấy dữ liệu số lượt check-in và nhãn cho ngày
        const checkinCounts = data.map((item) => item.checkInCount);

        // Cập nhật lại dailyCheckinData với mảng dữ liệu
        setDailyCheckinData(checkinCounts);
      } catch (error) {
        console.error("Error fetching check-in data:", error);
      }
    };

    fetchCheckinData();
    fetchMonthlyRevenueData();
    fetchPackageSold();
    fetchRevenueData();
    fetchRegistrationData();
  }, []);

  const filteredDailyCheckinData = {
    labels: selectedDay ? [dailyLabels[selectedDay - 1]] : dailyLabels,
    datasets: [
      {
        label: "Số lượt check-in",
        data: selectedDay
          ? [dailyCheckinData[selectedDay - 1]] // Hiển thị số lượt check-in cho ngày được chọn
          : dailyCheckinData, // Hiển thị toàn bộ dữ liệu check-in nếu không có ngày chọn
        backgroundColor: "rgba(75, 192, 192, 0.5)",
        borderColor: "rgba(75, 192, 192, 1)",
        borderWidth: 1,
      },
    ],
  };

  // Prepare filtered data for the chart
  const filteredMonthlyData = {
    labels: selectedMonth ? [monthlyLabels[selectedMonth - 1]] : monthlyLabels, // Filter by selected month if any
    datasets: [
      {
        label: "Tổng tiền theo tháng",
        data: selectedMonth ? [monthlyData[selectedMonth - 1]] : monthlyData, // Filter data by selected month
        backgroundColor: "rgba(54, 162, 235, 0.5)", // Bar color
        borderColor: "rgba(54, 162, 235, 1)", // Bar border color
        borderWidth: 1,
      },
    ],
  };

  // Function to generate random colors
  const generateColors = (numColors) => {
    const colors = [];
    for (let i = 0; i < numColors; i++) {
      const randomColor = `rgba(${Math.floor(Math.random() * 256)}, ${Math.floor(Math.random() * 256)}, ${Math.floor(Math.random() * 256)}, 0.5)`;
      colors.push(randomColor);
    }
    return colors;
  };

  // Package sales data
  const packageSalesData = {
    labels: packageSold.namePackage, // Package names as labels
    datasets: [
      {
        label: "Số lượng bán ra",
        data: packageSold.quantityPackage, // Package quantities as data
        backgroundColor: generateColors(packageSold.quantityPackage.length), // Generate dynamic colors
        borderColor: generateColors(packageSold.quantityPackage.length).map((color) => color.replace("0.5", "1")), // Border colors (same as background but fully opaque)
        borderWidth: 1,
      },
    ],
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
                  <p>{`${revenueData.revenueThisMonth.toLocaleString()} VND`}</p>
                  <span className={`stat-percentage ${revenueData.growthPercentage > 0 ? "text-success" : "text-danger"}`}>
                    {revenueData.growthPercentage > 0 ? "↑" : "↓"} {Math.abs(revenueData.growthPercentage)}%
                  </span>
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
                  <h4>Lượng gói bán được: {`${packageSold.totalPackage.toLocaleString()}`}</h4>
                  <ul>{packageSold.namePackage.length > 0 ? packageSold.namePackage.map((name, index) => <li key={index}>{`${name}: ${packageSold.quantityPackage[index]}`}</li>) : <li>Loading...</li>}</ul>
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
                  <h4>Lượt khách đăng ký</h4>
                  <p>{registrationData.registrationsThisMonth} Lượt</p>
                  <span className={`stat-percentage ${registrationData.growthPercentage > 0 ? "text-success" : "text-danger"}`}>
                    {registrationData.growthPercentage > 0 ? "↑" : "↓"} {Math.abs(registrationData.growthPercentage)}%
                  </span>
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
              <select id="monthFilter" className="form-select" value={selectedMonth} onChange={(e) => setSelectedMonth(e.target.value)}>
                <option value="">Tất cả tháng</option>
                {monthlyLabels.map((month, index) => (
                  <option key={index} value={index + 1}>
                    {month}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-6">
              <label htmlFor="dayFilter">Chọn ngày trong tuần:</label>
              <select id="dayFilter" className="form-select" value={selectedDay} onChange={(e) => setSelectedDay(e.target.value)}>
                <option value="">Tất cả các ngày</option>
                {dailyLabels.map((day, index) => (
                  <option key={index} value={index + 1}>
                    {day}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="row">
            {/* Biểu đồ Tổng tiền theo tháng */}
            <div className="col-md-6">
              <h4 className="chart-title">Biểu đồ doanh thu theo tháng</h4>
              <h4 className="chart-title">Doanh thu trong năm: {totalRevenueAllMonth} VND</h4>
              <div className="small-chart">
                <Bar data={filteredMonthlyData} options={options} />
              </div>
            </div>

            {/* Biểu đồ Check-in theo ngày */}
            <div className="col-md-6">
              <h4 className="chart-title">Biểu đồ Check-in theo ngày</h4>
              <h4 className="chart-title">Lượng Check-in trong tuần này: {dailyCheckinData.reduce((acc, curr) => acc + curr, 0)} lượt</h4>

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
