import React, { useState, useEffect, useRef } from "react";
import { useNavigate } from 'react-router-dom';
import axios from "axios";
import CloseIcon from "@mui/icons-material/Close";

import "bootstrap/dist/css/bootstrap.min.css";
import "./ManageSchedule.css";
import Header from "../Header/Header.js";

// Hàm lấy button cho lịch của huấn luyện viên
// Hàm lấy button cho lịch của huấn luyện viên

const ManageSchedule = () => {
  const navigate = useNavigate();
  const [allTrainerData, setAllTrainerData] = useState([]); // Store raw data fetched from API
  const [trainers, setTrainers] = useState([]); // Store trainers' data filtered by date
  const [loading, setLoading] = useState(true); // Loading state for the API call
  const [selectedDate, setSelectedDate] = useState(null); // Track selected date
  const timeSlots = ["6:00", "7:00", "8:00", "9:00", "10:00", "11:00", "12:00", "13:00", "14:00", "15:00", "16:00", "17:00", "18:00", "19:00", "20:00", "21:00"];

  // MODAL
  const [currentTrainer, setCurrentTrainer] = useState(null); // for editing trainer
  const viewDetailSlot = useRef(null);
  const BookTrainer = useRef(null);
  const [selectedOption, setSelectedOption] = useState(""); // to store selected option
  const [trainerToAddCourse, setTrainerToAddCourse] = useState(null); // for deletion
  const successModalRef = useRef(null);
  const [errors, setErrors] = useState({});
  const [rentalOptions, setRentalOptions] = useState([]);
  const [successMessage, setSuccessMessage] = useState("");

  const [gymPackages, setGymPackages] = useState([]); // Store Gym packages
  const [boxingPackages, setBoxingPackages] = useState([]); // Store Boxing packages
  const [selectedPackages, setSelectedPackages] = useState([]); // Store the packages based on selected radio

  const openViewDetailSlot = (trainerName, timeSlot, customers, rentalOption, boxingOption) => {
    const [startTime, endTime] = timeSlot.split("-");
  
    // If rentalOption is "No rental option", display boxingOption, and vice versa
    let optionToDisplay = "No option";
    if (rentalOption && rentalOption !== "No rental option") {
      optionToDisplay = rentalOption;
    } else if (boxingOption && boxingOption !== "No boxing option") {
      optionToDisplay = boxingOption;
    }
  
    setFormData({
      ...formData,
      trainerName,
      timeSlot,
      startTime,
      endTime,
      customers: customers.map((customer) => customer.name).join(", "),
      rentalOption, // Add rentalOption to formData
      boxingOption, // Add boxingOption to formData
      optionToDisplay, // Display the correct option
    });
  
    viewDetailSlot.current.style.display = "block";
    viewDetailSlot.current.classList.add("active");
    document.querySelector(".modal-overlay").style.display = "block";
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

  const [formData, setFormData] = useState({
    trainerId: "string",
    userId: "string",
    name: "string",
    isTrainerGym: true,
    isTrainerBoxing: true,
    bio: "string",
    specialization: "string",
  });

  const closeModal = () => {
    viewDetailSlot.current.style.display = "none";
    viewDetailSlot.current.classList.remove("active");
    document.querySelector(".modal-overlay").style.display = "none";
    setCurrentTrainer(null); // Đặt lại trainer hiện tại
    setFormData({
      trainerId: "string",
      userId: "string",
      name: "string",
      isTrainerGym: true,
      isTrainerBoxing: true,
      bio: "string",
      specialization: "string",
    });
  };

  const handleSubmitAddTrainerToCourse = async (e) => {
    e.preventDefault(); // Ngăn hành vi mặc định của form

    if (trainerToAddCourse && selectedOption) {
      const token = localStorage.getItem("token");

      // Chuẩn bị dữ liệu gửi tới API
      const data = {
        trainerRentalPlanId: "string", // Bạn cần gán ID hợp lệ tại đây nếu cần
        trainerId: trainerToAddCourse.trainerId, // ID của huấn luyện viên đã chọn
        rentalOptionId: selectedOption, // rentalOptionId là giá trị ID của gói đã chọn
      };

      try {
        // Gửi yêu cầu API để thêm huấn luyện viên vào khóa học
        const response = await axios.post(
          "http://localhost:5000/api/trainerRentalPlan",
          data, // Gửi payload
          {
            headers: { Authorization: `Bearer ${token}` }, // Thêm token vào header
          }
        );

        // Hiển thị thông báo thành công và đóng modal
        closeModal(); // Đóng modal
        showSuccessModal("Huấn luyện viên đã được thêm vào khóa học thành công!");
      } catch (error) {
        console.error("Error adding trainer to course:", error);
        showSuccessModal("Có lỗi khi thêm huấn luyện viên vào khóa học.");
      }
    } else {
      // Nếu không chọn huấn luyện viên hoặc gói, hiển thị thông báo lỗi
      showSuccessModal("Vui lòng chọn huấn luyện viên và gói!");
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;

    // Update the form data
    setFormData({
      ...formData,
      [name]: value,
    });

    // When the radio button for trainer type is clicked
    if (name === "trainerType") {
      setSelectedOption(""); // Reset the selected package before fetching new ones

      // Fetch the packages based on the selected trainer type
      fetchPackages(value); // Call fetchPackages with "Gym" or "Boxing"
    }
  };

  // Fetch gym or boxing packages based on selection
  const fetchPackages = async (type) => {
    try {
      const token = localStorage.getItem("token");
      if (!token) {
        console.error("No token found in localStorage");
        return;
      }

      const response = await axios.get(`http://localhost:5000/api/PackagesAndTrainers?type=${type}`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      // Update selected packages based on type
      if (type === "Gym") {
        setSelectedPackages(response.data); // Set Gym packages for selection
      } else if (type === "Boxing") {
        setSelectedPackages(response.data); // Set Boxing packages for selection
      }
    } catch (error) {
      console.error("Error fetching packages:", error);
    }
  };

  useEffect(() => {
    // Optionally fetch trainers or other data initially
    const fetchData = async () => {
      try {
        const token = localStorage.getItem("token");
        if (!token) {
          console.error("No token found in localStorage");
          return;
        }

        // Fetch gym and boxing packages initially (without triggering via radio button)
        const [gymResponse, boxingResponse] = await Promise.all([
          axios.get("http://localhost:5000/api/PackagesAndTrainers?type=TrainerRental", {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }),
          axios.get("http://localhost:5000/api/PackagesAndTrainers?type=Boxing", {
            headers: {
              Authorization: `Bearer ${token}`,
            },
          }),
        ]);

        setGymPackages(gymResponse.data); // Set gym packages
        setBoxingPackages(boxingResponse.data); // Set boxing packages
      } catch (error) {
        console.error("Error fetching data:", error);
      }
    };

    fetchData();
  }, []);

  const handleRegisterTrainer = () => {
    // Mở tab mới và chuyển đến URL bạn muốn
    window.open("http://localhost:3000/bookTrainerForm", "_blank");
  };

  // END MODAL

  // SCHEDULE
  const getScheduleButton = (trainerName, timeSlot, selectedDate, trainers) => {
    const trainer = trainers.find((trainer) => trainer.trainerName === trainerName);
    if (trainer && Array.isArray(trainer.slots)) {
      const schedule = trainer.slots.filter((slot) => {
        const startTime = slot.timeSlot.split("-")[0];
        return slot.date === selectedDate && startTime === timeSlot;
      });

      const hasCustomers = schedule.length > 0 && schedule[0].customers.length > 0;
      const customers = hasCustomers ? schedule[0].customers : [];
      const rentalOption = schedule.length > 0 ? schedule[0].rentalOption : "No rental option";
      const boxingOption = schedule.length > 0 ? schedule[0].boxingOption : "No boxing option";

      if (hasCustomers) {
        return (
          <button onClick={() => openViewDetailSlot(trainerName, schedule[0].timeSlot, customers, rentalOption, boxingOption)} className="btn btn-info btn-sm">
            Chi tiết
          </button>
        );
      } else {
        // Display "Trống" when there are no customers
        return <span className="">Trống</span>;
      }
    }
    // Fallback for when there is no trainer data
    return <span className="">Trống</span>;
  };

  //END SCHEDULE

  // Hàm lấy dữ liệu huấn luyện viên từ API
  useEffect(() => {
    const fetchTrainerData = async () => {
      try {
        const token = localStorage.getItem("token");
        if (!token) {
          console.error("No token found in localStorage");
          return;
        }
        const response = await fetch("http://localhost:5000/api/Schedule/Slots/All", {
          method: "GET",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
        });
        if (!response.ok) {
          throw new Error("Failed to fetch trainer data");
        }
        const data = await response.json();
        setAllTrainerData(data); // Lưu toàn bộ dữ liệu gốc
        setTrainers(data); // Hiển thị ban đầu với tất cả dữ liệu
        setLoading(false);
      } catch (error) {
        console.error("Error fetching trainer data:", error);
        setLoading(false);
      }
    };

    fetchTrainerData();
  }, []);

  const handleDateChange = (e) => {
    const selected = e.target.value; // Lấy ngày được chọn
    setSelectedDate(selected);

    // Lọc dữ liệu theo ngày được chọn
    const filteredData = allTrainerData.map((trainer) => {
      // Kiểm tra nếu trainer.slots tồn tại và là mảng
      const filteredSlots = Array.isArray(trainer.slots) ? trainer.slots.filter((slot) => slot.date === selected) : []; // Nếu không có slots, trả về mảng rỗng
      return { ...trainer, slots: filteredSlots };
    });
    setTrainers(filteredData); // Cập nhật dữ liệu hiển thị theo ngày
  };

  return (
    <>
      <Header />

      {/* Schedule Section */}
      <section className="section" id="schedule">
        <div className="container">
          <div className="row">
            <div className="col-lg-6 offset-lg-3">
              <div className="section-heading-home dark-bg">
                <h2>
                  Quản lý <em>Lịch tập</em>
                </h2>
                {/* Date Picker */}
                <section>
                  <div className="container">
                    <div className="row">
                      <div className="col-lg-6 offset-lg-3">
                        <input
                          type="date"
                          onChange={handleDateChange} // Handle date change
                          placeholder="Chọn ngày trong tuần"
                        />
                      </div>
                    </div>
                  </div>
                  {/* Before removing */}
                  {/* <button
                    className="btn btn-success btn-sm"
                    onClick={openBookTrainerModal} // No longer passing trainer
                  >
                    Đăng kí Trainer
                  </button> */}
                  <button className="btn btn-success btn-sm" onClick={handleRegisterTrainer}>Đăng ký Trainer</button>

                  {/* After removing (just remove the button) */}
                </section>
              </div>
            </div>
          </div>
          <div className="row">
            <div className="col">
              <div className="schedule-table filtering">
                <table>
                  <thead>
                    <tr>
                      <th></th> {/* Empty cell for trainer names */}
                      {timeSlots.map((slot, index) => (
                        <th key={index} style={{ textAlign: "center", color: "white" }}>
                          {slot}
                        </th>
                      ))}
                    </tr>
                  </thead>

                  <tbody>
                    {trainers.map((trainer, trainerIndex) => (
                      <tr key={trainerIndex}>
                        <td className="day-time">{trainer.trainerName}</td>
                        {timeSlots.map((slot, timeSlotIndex) => {
                          // Render nút "Chi tiết" hoặc "Thêm" theo thời gian và ngày đã chọn
                          const button = getScheduleButton(trainer.trainerName, slot, selectedDate, trainers);
                          return (
                            <td key={timeSlotIndex}>
                              <div className="schedule-item">{button}</div>
                            </td>
                          );
                        })}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* View Detail Slot */}
      <div ref={viewDetailSlot} className="modal">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <form id="addTrainerToCourseForm" onSubmit={handleSubmitAddTrainerToCourse}>
              <div className="modal-header">
                <h4 className="modal-title text-center mx-auto">
                  <span style={{ color: "red" }}>
                    Chi tiết slot của {formData.trainerName} vào lúc {formData.startTime} - {formData.endTime}
                  </span>
                </h4>
                <a type="button" className="close" onClick={closeModal}>
                  <CloseIcon />
                </a>
              </div>

              <div className="modal-body">
                <div className="row">
                  <div className="form-group col">
                    <label>
                      Tên khách hàng<span className="icon-input">(*)</span>
                    </label>
                    <textarea className="form-control" name="customers" value={formData.customers || ""} onChange={handleInputChange} required disabled />
                  </div>
                </div>

                <div className="row">
                  <div className="form-group col">
                    <label>Gói đăng kí</label>
                    <input
                      type="text"
                      className="form-control"
                      value={formData.optionToDisplay || ""} // Hiển thị gói hợp lệ
                      disabled
                    />
                  </div>
                </div>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Modal Overlay */}
      <div className="modal-overlay"></div>
    </>
  );
};

export default ManageSchedule;
