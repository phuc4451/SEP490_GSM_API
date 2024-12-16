import React, { useState, useEffect, useRef } from "react";
import "bootstrap/dist/css/bootstrap.min.css";
import { useNavigate } from "react-router-dom";
import { getRole, Logout, hasRequiredRole } from "../utils/authUtils";
import CloseIcon from "@mui/icons-material/Close";

import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faBackward, faDownload } from "@fortawesome/free-solid-svg-icons";
import { faTwitter, faFacebook, faSquareInstagram } from "@fortawesome/free-brands-svg-icons";
import "./home.css";
import autoplayVideo from "../assets/images/gym-video.mp4";
import supergym from "../assets/images/super-gym-logo.jpg";
import lineDec from "../assets/images/line-dec.png";
import apkApp from "../assets/images/apkAppfix.png";
import featureFirstIcon from "../assets/images/features-first-icon.png";
import tabFirstIcon from "../assets/images/tabs-first-icon.png";
import trainingClass1 from "../assets/images/training-image-01.jpg";
import trainingClass2 from "../assets/images/training-image-02.jpg";
import trainingClass3 from "../assets/images/training-image-03.jpg";
import trainingClass4 from "../assets/images/training-image-04.jpg";
import trainer1 from "../assets/images/first-trainer.jpg";
import trainer2 from "../assets/images/second-trainer.jpg";
import trainer3 from "../assets/images/third-trainer.jpg";
import khanh from "../assets/images/khanh.jpg";
import ngoc from "../assets/images/ngoc.jpg";
import nam from "../assets/images/nam.jpg";

const Home = () => {
  // FIX CHANGE COLOR SECTIONS
  const [activeSection, setActiveSection] = useState("home");

  // Hàm xử lý khi nhấn vào mục nào đó
  const handleSetActive = (section) => {
    setActiveSection(section); // Cập nhật ngay mục active khi nhấn vào menu
    const sectionElement = document.querySelector(`#${section}`);
    if (sectionElement) {
      sectionElement.scrollIntoView({ behavior: "smooth" });
    }
  };

  // Xử lý khi cuộn để tự động đổi màu header dựa trên section trong viewport
  useEffect(() => {
    const handleScroll = () => {
      const sections = document.querySelectorAll("section"); // Lấy tất cả các section
      let currentSection = "home";

      sections.forEach((section) => {
        const sectionTop = section.offsetTop - 50; // Trừ đi một ít giá trị để bù trừ cho header
        const sectionHeight = section.clientHeight;

        // Cập nhật section nếu cuộn vào vị trí trong khoảng của nó
        if (window.scrollY >= sectionTop && window.scrollY < sectionTop + sectionHeight) {
          currentSection = section.getAttribute("id");
        }
      });

      setActiveSection(currentSection); // Cập nhật section hiện tại
    };

    window.addEventListener("scroll", handleScroll);

    return () => {
      window.removeEventListener("scroll", handleScroll);
    };
  }, []);

  // =================================================================

  //FIX SCHEDULE
  const [activeFilter, setActiveFilter] = useState("monday");
  const handleFilterClick = (filter) => {
    setActiveFilter(filter); // Update active filter state
  };
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

  const getSchedule = (classType) => {
    return scheduleData[activeFilter][classType] || ["", ""];
  };

  // =================================================================

  //FIX CLASSES
  const [activeTab, setActiveTab] = useState(1);
  // =================================================================

  //FIX STICKY HEADER
  const [isSticky, setIsSticky] = useState(false);
  // =================================================================

  // // FIX CHOOSING SECTION HEADER
  // const [activeSection, setActiveSection] = useState("home");

  // // Hàm xử lý khi nhấn vào mục nào đó
  // const handleSetActive = (section) => {
  //     setActiveSection(section); // Cập nhật mục active
  // };
  // =================================================================
  //FIX STICKY HEADER

  // OPEN QR
  const showQrPicture = useRef(null);
  const [qrDataUrl, setQrDataUrl] = useState(""); // State to store the QR code data URL

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

  // END QR

  //FIX PRELOADER
  useEffect(() => {
    // Thời gian hiển thị preloader (ví dụ: 2 giây)
    const timer = setTimeout(() => {
      const preloader = document.querySelector(".js-preloader");
      if (preloader) {
        preloader.classList.add("loaded"); // Thêm className "loaded" để ẩn preloader
      }
    }, 1000); // 2 giây

    return () => clearTimeout(timer); // Xóa timer khi component unmount
  }, []);

  //   const userRole = getRole();
  //   const canShowManangeButton = () => {
  //     return ["admin", "staff"].includes(userRole);
  //   };
  return (
    <>
      {/* <!-- ***** Preloader Start ***** --> */}
      <div id="js-preloader" className="js-preloader">
        <div className="preloader-inner">
          <span className="dot"></span>
          <div className="dots">
            <span></span>
            <span></span>
            <span></span>
          </div>
        </div>
      </div>
      {/* <!-- ***** Preloader End ***** --> */}

      {/* <!-- ***** Header Area Start ***** --> */}
      <header className={`header-area ${isSticky ? "background-header" : ""}`}>
        <div className="container">
          <div className="row">
            <div className="col-12">
              <nav className="main-nav">
                <div className="back-dashboard">
                  {/* {hasRequiredRole(["admin", "staff"]) && ( */}
                  <div className="main-button manage-button pe-4">
                    <a href="/login">
                      <FontAwesomeIcon icon={faBackward} /> quản lí
                    </a>
                  </div>
                  {/* )} */}

                  <a href="index.html" className="logo">
                    <em>Super Gym </em>
                    <img src={supergym} alt="logo" className="img-supergym" />
                    Hòa lạc
                  </a>
                </div>

                <ul className="nav">
                  <li className="scroll-to-section">
                    <a href="#top" className={activeSection === "home" ? "active" : ""} onClick={() => handleSetActive("home")}>
                      Trang chính
                    </a>
                  </li>
                  <li className="scroll-to-section">
                    <a href="#features" className={activeSection === "features" ? "active" : ""} onClick={() => handleSetActive("features")}>
                      Giới thiệu
                    </a>
                  </li>
                  <li className="scroll-to-section">
                    <a href="#our-classes" className={activeSection === "our-classes" ? "active" : ""} onClick={() => handleSetActive("our-classes")}>
                      Lớp tập
                    </a>
                  </li>
                  <li className="scroll-to-section">
                    <a href="#schedule" className={activeSection === "schedule" ? "active" : ""} onClick={() => handleSetActive("schedule")}>
                      Lịch tập
                    </a>
                  </li>
                  <li className="scroll-to-section">
                    <a href="#contact-us" className={activeSection === "contact-us" ? "active" : ""} onClick={() => handleSetActive("contact-us")}>
                      Liên hệ
                    </a>
                  </li>
                  <li className="main-button">
                    <a href="/signup">Đăng kí</a>
                  </li>
                </ul>

                <a className="menu-trigger">
                  <span>Menu</span>
                </a>
              </nav>
            </div>
          </div>
        </div>
      </header>

      {/* <!-- ***** Header Area End ***** --> */}

      {/* <!-- ***** Main Banner Area Start ***** --> */}
      <div className="main-banner" id="top">
        <video autoPlay muted loop id="bg-video">
          <source src={autoplayVideo} type="video/mp4" />
          Your browser does not support the video tag.
        </video>

        <div className="video-overlay header-text">
          <div className="caption">
            <h6>work harder, get stronger</h6>
            <h2>
              Đồng hành cùng với <em>Super gym</em>
            </h2>
            {/* <div className="main-button scroll-to-section"> */}
            <div className="main-button">
              {/* <a href="#features"> */}
              <a href="#">
                Đăng kí thành viên <FontAwesomeIcon icon={faDownload} />
              </a>
            </div>
            <img src={apkApp} className="img-fluid w-10 mt-4" style={{ maxWidth: "10%" }} alt="Super Gym App" />
          </div>
        </div>
      </div>
      {/* <!-- ***** Main Banner Area End ***** --> */}

      {/* <!-- ***** Features Item Start ***** --> */}
      <section className="section" id="features">
  <div className="container">
    <div className="row">
      <div className="col-lg-6 offset-lg-3">
        <div className="section-heading-home">
          <h2>
            Chọn <em>chương trình</em>
          </h2>
          <img src={lineDec} alt="đường kẻ" />
          <p>Khám phá các chương trình tập luyện đa dạng, được thiết kế phù hợp với mọi mục tiêu và trình độ.</p>
        </div>
      </div>
      <div className="col-lg-6">
        <ul className="features-items">
          <li className="feature-item">
            <div className="left-icon">
              <img src={featureFirstIcon} alt="Tập luyện cơ bản" />
            </div>
            <div className="right-content">
              <h4>Tập Luyện Cơ Bản</h4>
              <p>Chương trình dành cho người mới bắt đầu, giúp bạn làm quen với các bài tập và xây dựng nền tảng thể chất vững chắc.</p>
              <a href="#" className="text-button">
                Tìm Hiểu Thêm
              </a>
            </div>
          </li>
          <li className="feature-item">
            <div className="left-icon">
              <img src={featureFirstIcon} alt="Tập luyện gym mới" />
            </div>
            <div className="right-content">
              <h4>Tập Luyện Gym Nâng Cao</h4>
              <p>Chương trình tập luyện hiện đại với các thiết bị mới nhất, giúp bạn đạt hiệu quả tối ưu trong quá trình rèn luyện.</p>
              <a href="#" className="text-button">
                Tìm Hiểu Thêm
              </a>
            </div>
          </li>
          <li className="feature-item">
            <div className="left-icon">
              <img src={featureFirstIcon} alt="Khóa cơ bắp cơ bản" />
            </div>
            <div className="right-content">
              <h4>Khóa Học Cơ Bắp Cơ Bản</h4>
              <p>Chương trình tập trung vào phát triển cơ bắp cho người mới, với các bài tập an toàn và hiệu quả.</p>
              <a href="#" className="text-button">
                Tìm Hiểu Thêm
              </a>
            </div>
          </li>
        </ul>
      </div>
      <div className="col-lg-6">
        <ul className="features-items">
          <li className="feature-item">
            <div className="left-icon">
              <img src={featureFirstIcon} alt="Khóa cơ bắp nâng cao" />
            </div>
            <div className="right-content">
              <h4>Khóa Học Cơ Bắp Nâng Cao</h4>
              <p>Chương trình chuyên sâu về phát triển cơ bắp, dành cho những người đã có nền tảng tập luyện vững chắc.</p>
              <a href="#" className="text-button">
                Tìm Hiểu Thêm
              </a>
            </div>
          </li>
          <li className="feature-item">
            <div className="left-icon">
              <img src={featureFirstIcon} alt="Tập yoga" />
            </div>
            <div className="right-content">
              <h4>Lớp Học Yoga</h4>
              <p>Kết hợp các bài tập thể chất và tinh thần, giúp bạn cân bằng cuộc sống và cải thiện sức khỏe toàn diện.</p>
              <a href="#" className="text-button">
                Tìm Hiểu Thêm
              </a>
            </div>
          </li>
          <li className="feature-item">
            <div className="left-icon">
              <img src={featureFirstIcon} alt="Khóa thể hình" />
            </div>
            <div className="right-content">
              <h4>Khóa Học Thể Hình</h4>
              <p>Chương trình toàn diện giúp bạn đạt được vóc dáng mơ ước thông qua các bài tập chuyên sâu và chế độ dinh dưỡng phù hợp.</p>
              <a href="#" className="text-button">
                Tìm Hiểu Thêm
              </a>
            </div>
          </li>
        </ul>
      </div>
    </div>
  </div>
</section>

<section className="section" id="call-to-action">
  <div className="container">
    <div className="row">
      <div className="col-lg-10 offset-lg-1">
        <div className="cta-content">
          <h2>
            Đừng <em>chần chừ</em>, hãy bắt đầu <em>ngay hôm nay</em>!
          </h2>
          <p>Không có thời điểm nào tốt hơn để bắt đầu hành trình rèn luyện sức khỏe của bạn. Hãy để chúng tôi đồng hành cùng bạn trên con đường đạt được mục tiêu.</p>
          <div className="main-button scroll-to-section">
            <a href="#our-classes">Đăng ký thành viên</a>
          </div>
        </div>
      </div>
    </div>
  </div>
</section>

<section className="section" id="our-classes">
  <div className="container">
    <div className="row">
      <div className="col-lg-6 offset-lg-3">
        <div className="section-heading-home">
          <h2>
            Các <em>Lớp Học</em> Của Chúng Tôi
          </h2>
          <img src={lineDec} alt="đường kẻ" />
          <p>Khám phá đa dạng các lớp học được thiết kế phù hợp với mọi mục tiêu và trình độ của bạn.</p>
        </div>
      </div>
    </div>
    <div className="row" id="tabs">
      <div className="col-lg-4">
        <ul>
          <li>
            <a
              href="#tabs-1"
              onClick={(e) => {
                e.preventDefault();
                setActiveTab(1);
              }}
            >
              <img src={tabFirstIcon} alt="Biểu tượng lớp học 1" /> Lớp Tập Cơ Bản
            </a>
          </li>
          <li>
            <a
              href="#tabs-2"
              onClick={(e) => {
                e.preventDefault();
                setActiveTab(2);
              }}
            >
              <img src={tabFirstIcon} alt="Biểu tượng lớp học 2" /> Lớp Tập Nâng Cao
            </a>
          </li>
          <li>
            <a
              href="#tabs-3"
              onClick={(e) => {
                e.preventDefault();
                setActiveTab(3);
              }}
            >
              <img src={tabFirstIcon} alt="Biểu tượng lớp học 3" /> Lớp Cardio
            </a>
          </li>
          <li>
            <a
              href="#tabs-4"
              onClick={(e) => {
                e.preventDefault();
                setActiveTab(4);
              }}
            >
              <img src={tabFirstIcon} alt="Biểu tượng lớp học 4" /> Lớp Yoga
            </a>
          </li>
          <div className="main-rounded-button">
            <a href="#">Xem Tất Cả Lịch Học</a>
          </div>
        </ul>
      </div>
      <div className="col-lg-8">
        <section className="tabs-content">
          {activeTab === 1 && (
            <article id="tabs-1">
              <img src={trainingClass1} alt="Lớp tập cơ bản" />
              <h4>Lớp Tập Cơ Bản</h4>
              <p>Khóa học dành cho người mới bắt đầu, giúp bạn làm quen với các bài tập cơ bản...</p>
              <div className="main-button">
                <a href="#">Xem Lịch Học</a>
              </div>
            </article>
          )}
          {activeTab === 2 && (
            <article id="tabs-2">
              <img src={trainingClass2} alt="Lớp tập nâng cao" />
              <h4>Lớp Tập Nâng Cao</h4>
              <p>Chương trình tập luyện chuyên sâu dành cho những người đã có nền tảng...</p>
              <div className="main-button">
                <a href="#">Xem Lịch Học</a>
              </div>
            </article>
          )}
          {activeTab === 3 && (
            <article id="tabs-3">
              <img src={trainingClass3} alt="Lớp cardio" />
              <h4>Lớp Cardio</h4>
              <p>Tăng cường sức bền và đốt cháy mỡ thừa hiệu quả...</p>
              <div className="main-button">
                <a href="#">Xem Lịch Học</a>
              </div>
            </article>
          )}
          {activeTab === 4 && (
            <article id="tabs-4">
              <img src={trainingClass4} alt="Lớp yoga" />
              <h4>Lớp Yoga</h4>
              <p>Kết hợp các bài tập thể chất và tinh thần để cân bằng cuộc sống...</p>
              <div className="main-button">
                <a href="#">Xem Lịch Học</a>
              </div>
            </article>
          )}
        </section>
      </div>
    </div>
  </div>
</section>
      {/* <!-- ***** Our Classes End ***** --> */}
      {/* SCHEDULE START */}
      {/* <section className="section" id="schedule">
                <div className="container">
                    <div className="row">
                        <div className="col-lg-6 offset-lg-3">
                            <div className="section-heading-home dark-bg">
                                <h2>Classes <em>Schedule</em></h2>
                                <img src="assets/images/line-dec.png" alt="" />
                                <p>Nunc urna sem, laoreet ut metus id, aliquet consequat magna. Sed viverra ipsum dolor, ultricies fermentum massa consequat eu.</p>
                            </div>
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-lg-12">
                            <div className="filters">
                                <ul className="schedule-filter">
                                    <li className={activeFilter === 'monday' ? 'active' : ''} onClick={() => handleFilterClick('monday')}>Monday</li>
                                    <li className={activeFilter === 'tuesday' ? 'active' : ''} onClick={() => handleFilterClick('tuesday')}>Tuesday</li>
                                    <li className={activeFilter === 'wednesday' ? 'active' : ''} onClick={() => handleFilterClick('wednesday')}>Wednesday</li>
                                    <li className={activeFilter === 'thursday' ? 'active' : ''} onClick={() => handleFilterClick('thursday')}>Thursday</li>
                                    <li className={activeFilter === 'friday' ? 'active' : ''} onClick={() => handleFilterClick('friday')}>Friday</li>
                                </ul>
                            </div>
                        </div>
                        <div className="col-lg-10 offset-lg-1">
                            <div className="schedule-table filtering">
                                <table>
                                    <tbody>
                                        <tr>
                                            <td className="day-time">Fitness className</td>
                                            <td>{getSchedule('fitnessClass')[0] || ''}</td>
                                            <td>{getSchedule('fitnessClass')[1] || ''}</td>
                                            <td>William G. Stewart</td>
                                        </tr>
                                        <tr>
                                            <td className="day-time">Muscle Training</td>
                                            <td>{getSchedule('muscleTraining')[0] || ''}</td>
                                            <td>{getSchedule('muscleTraining')[1] || ''}</td>
                                            <td>Paul D. Newman</td>
                                        </tr>
                                        <tr>
                                            <td className="day-time">Body Building</td>
                                            <td>{getSchedule('bodyBuilding')[0] || ''}</td>
                                            <td>{getSchedule('bodyBuilding')[1] || ''}</td>
                                            <td>Boyd C. Harris</td>
                                        </tr>
                                        <tr>
                                            <td className="day-time">Yoga Training className</td>
                                            <td>{getSchedule('yogaClass')[0] || ''}</td>
                                            <td>{getSchedule('yogaClass')[1] || ''}</td>
                                            <td>Hector T. Daigle</td>
                                        </tr>
                                        <tr>
                                            <td className="day-time">Advanced Training</td>
                                            <td>{getSchedule('advancedTraining')[0] || ''}</td>
                                            <td>{getSchedule('advancedTraining')[1] || ''}</td>
                                            <td>Bret D. Bowers</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </section> */}
      {/* SCHEDULE END */}

      {/* <!-- ***** Testimonials Starts ***** --> */}
      <section className="section" id="trainers">
  <div className="container">
    <div className="row">
      <div className="col-lg-6 offset-lg-3">
        <div className="section-heading-home">
          <h2>
            Đội Ngũ <em>Huấn Luyện Viên</em> Chuyên Nghiệp
          </h2>
          <img src={lineDec} alt="" />
          <p>Chúng tôi tự hào giới thiệu đội ngũ huấn luyện viên giàu kinh nghiệm, tận tâm hỗ trợ bạn đạt được mục tiêu sức khỏe và thể hình của mình.</p>
        </div>
      </div>
    </div>
    <div className="row">
      <div className="col-lg-4">
        <div className="trainer-item">
          <div className="image-thumb">
            <img src={khanh} alt="Huấn luyện viên Nguyễn Ngọc Khánh" />
          </div>
          <div className="down-content">
            <span>Huấn Luyện Viên Sức Mạnh</span>
            <h4>Nguyễn Ngọc Khánh</h4>
            <p>Chuyên gia về các bài tập sức mạnh với hơn 5 năm kinh nghiệm. Đam mê giúp học viên phát triển sức mạnh và độ bền một cách toàn diện.</p>
            <ul className="social-icons">
              <li>
                <a href="#">
                  <i className="fa fa-facebook"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-twitter"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-linkedin"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-behance"></i>
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>
      <div className="col-lg-4">
        <div className="trainer-item">
          <div className="image-thumb">
            <img src={ngoc} alt="Huấn luyện viên Đặng Ngọc" />
          </div>
          <div className="down-content">
            <span>Huấn Luyện Viên Cơ Bắp</span>
            <h4>Đặng Ngọc</h4>
            <p>Chuyên gia về phát triển cơ bắp và định hình cơ thể. Với kiến thức chuyên sâu về dinh dưỡng và tập luyện, sẽ giúp bạn đạt được vóc dáng mơ ước.</p>
            <ul className="social-icons">
              <li>
                <a href="#">
                  <i className="fa fa-facebook"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-twitter"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-linkedin"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-behance"></i>
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>
      <div className="col-lg-4">
        <div className="trainer-item">
          <div className="image-thumb">
            <img src={nam} alt="Huấn luyện viên Nguyễn Lý Nam" />
          </div>
          <div className="down-content">
            <span>Huấn Luyện Viên Thể Lực</span>
            <h4>Nguyễn Lý Nam</h4>
            <p>Chuyên gia về rèn luyện thể lực và sức bền. Với phương pháp huấn luyện khoa học, sẽ giúp bạn nâng cao thể lực và sức khỏe toàn diện.</p>
            <ul className="social-icons">
              <li>
                <a href="#">
                  <i className="fa fa-facebook"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-twitter"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-linkedin"></i>
                </a>
              </li>
              <li>
                <a href="#">
                  <i className="fa fa-behance"></i>
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</section>
      {/* <!-- ***** Testimonials Ends ***** --> */}

      {/* <!-- ***** Contact Us Area Starts ***** --> */}
      <section className="section" id="contact-us">
        <div className="container-fluid">
          <div className="row">
            <div className="col-lg-6 col-md-6 col-xs-12">
              <div id="map">
                <iframe src="https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3724.414354987037!2d105.51678507471412!3d21.016100388214372!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x31345be8d22636ff%3A0x67b6b13529b8e83e!2zU3VwZXIgR3ltIEhvw6AgTOG6oWMgMg!5e0!3m2!1svi!2s!4v1729650914182!5m2!1svi!2s" width="100%" height="600px" style={{ border: 0 }} allowFullScreen=""></iframe>
              </div>
            </div>
            <div className="col-lg-6 col-md-6 col-xs-12">
              <div className="contact-form">
                <form id="contact" action="" method="post">
                  <div className="row">
                    <div className="col-md-6 col-sm-12">
                      <fieldset>
                        <input name="name" type="text" id="name" placeholder="Your Name*" required />
                      </fieldset>
                    </div>
                    <div className="col-md-6 col-sm-12">
                      <fieldset>
                        <input name="email" type="text" id="email" pattern="[^ @]*@[^ @]*" placeholder="Your Email*" required />
                      </fieldset>
                    </div>
                    <div className="col-md-12 col-sm-12">
                      <fieldset>
                        <input name="subject" type="text" id="subject" placeholder="Subject" />
                      </fieldset>
                    </div>
                    <div className="col-lg-12">
                      <fieldset>
                        <textarea name="message" rows="6" id="message" placeholder="Message" required></textarea>
                      </fieldset>
                    </div>
                    <div className="col-lg-12">
                      <fieldset>
                        <button type="submit" id="form-submit" className="main-button">
                          Send Message
                        </button>
                      </fieldset>
                    </div>
                  </div>
                </form>
              </div>
            </div>
          </div>
        </div>
      </section>
      {/* <!-- ***** Contact Us Area Ends ***** --> */}

      {/* <!-- ***** Footer Start ***** --> */}
      <footer>
        <div className="container">
          <div className="row">
            <div className="col-lg-6 col-md-6">
              <div className="footer-widget">
                <h4>SUPER GYM</h4>
                <p>Super Gym là trung tâm thể hình hiện đại, cung cấp đa dạng dịch vụ như tập gym, yoga, và fitness với trang thiết bị tiên tiến cùng đội ngũ huấn luyện viên chuyên nghiệp. Đây là điểm đến lý tưởng cho mọi người muốn cải thiện sức khỏe và vóc dáng.</p>
                <div className="footer-social">
                  <a href="#">
                    <FontAwesomeIcon icon={faTwitter} />
                  </a>
                  <a href="#">
                    <FontAwesomeIcon icon={faFacebook} />
                  </a>
                  <a href="#">
                    <FontAwesomeIcon icon={faSquareInstagram} />
                  </a>
                </div>
              </div>
            </div>
            {/* <div className="col-lg-3 col-md-6">
              <div className="footer-widget">
                <h4>Latest News</h4>
                <ul className="footer-news">
                  <li>
                    <img src={trainer1} alt="news" />
                    <p>Even the all-powerful Pointing has no control about</p>
                    <span>Oct. 16, 2019 | Admin | 19</span>
                  </li>
                  <li>
                    <img src={trainingClass1} alt="news" />
                    <p>Even the all-powerful Pointing has no control about</p>
                    <span>Oct. 16, 2019 | Admin | 19</span>
                  </li>
                </ul>
              </div>
            </div> */}
            {/* <div className="col-lg-3 col-md-6">
              <div className="footer-widget">
                <h4>Quick Links</h4>
                <ul className="footer-links">
                  <li>
                    <a href="#">Home</a>
                  </li>
                  <li>
                    <a href="#">About</a>
                  </li>
                  <li>
                    <a href="#">Services</a>
                  </li>
                  <li>
                    <a href="#">Works</a>
                  </li>
                  <li>
                    <a href="#">Blog</a>
                  </li>
                  <li>
                    <a href="#">Contact</a>
                  </li>
                </ul>
              </div>
            </div> */}
            <div className="col-lg-6 col-md-6">
              <div className="footer-widget">
                <h4>ĐỊA CHỈ/LIÊN LẠC</h4>
                <ul className="footer-contact">
                  <li>
                    <i className="fa-solid fa-marker"></i> Super Gym Hoà Lạc 2
                    396 thôn 4, Thạch Hoà, Thạch Thất, Hà Nội
                  </li>
                  <li>
                    <i className="fa-solid fa-phone"></i> +2 392 3929 210
                  </li>
                  <li>
                    <i className="fas fa-envelope"></i> SuperGymhoal@gmail.com
                  </li>
                </ul>
              </div>
            </div>
          </div>
          {/* <div className="row">
            <div className="col-lg-12 text-center">
              <p>
                Copyright &copy; 2024 All rights reserved | This template is made with <i className="fas fa-heart"></i> by{" "}
                <a href="https://colorlib.com" target="_blank">
                  Colorlib
                </a>
              </p>
            </div>
          </div> */}
        </div>
      </footer>

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

      {/* Modal Overlay */}
      <div className="modal-overlay"></div>
    </>
  );
};

export default Home;
