using Microsoft.AspNetCore.Mvc;
using Firebase.Database;
using Alpha_API.Models;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Authorization;
using ExcelDataReader;
using System.Data;
using System.Text;
[Route("api/[controller]")]
[ApiController]

[Authorize(Roles = "admin,staff")]
public class EquipmentController : ControllerBase
{
    private readonly FirebaseClient _firebaseClient;

    public EquipmentController()
    {
        _firebaseClient = new FirebaseClient("https://sgm-management-c98cd-default-rtdb.firebaseio.com/");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetEquipments()
    {
        // Lấy danh sách equipment và training rooms
        var equipmentsTask = _firebaseClient.Child("equipment").OnceAsync<Equipment>();
        var trainingRoomsTask = _firebaseClient.Child("TrainingRoom").OnceAsync<TrainingRoom>();
        var EquipmentCategoryTask = _firebaseClient.Child("EquipmentCategory").OnceAsync<EquipmentCategory>();


        await Task.WhenAll(equipmentsTask, trainingRoomsTask, EquipmentCategoryTask); // Đợi cả hai task hoàn thành

        var equipments = equipmentsTask.Result;
        var trainingRooms = trainingRoomsTask.Result.ToDictionary(tr => tr.Object.TrainingRoomId, tr => tr.Object.TrainingRoomName);
        var equipmentCategory = EquipmentCategoryTask.Result.ToDictionary(tr => tr.Object.EquipmentCategoryId, tr => tr.Object.EquipmentCategoryName);


        // Kết hợp dữ liệu Equipment với TrainingRoomName
        var equipmentList = equipments.Select(equipment => new
        {
            EquipmentId = equipment.Key,
            equipment.Object.EquipmentName,
            equipment.Object.EquipmentCode,
            equipment.Object.EquipmentImportPrice,
            equipment.Object.EquipmentBrand,
            equipment.Object.EquipmentQuantity,

            equipment.Object.EquipmentCategoryId,
            EquipmentCategoryName = equipmentCategory.GetValueOrDefault(equipment.Object.EquipmentCategoryId, "Unknown"),



            equipment.Object.TrainingRoomId,
            TrainingRoomName = trainingRooms.GetValueOrDefault(equipment.Object.TrainingRoomId, "Unknown"),

            equipment.Object.EquipmentManufactured,
            equipment.Object.EquipmentSize,
            equipment.Object.EquipmentWeightStack,
            equipment.Object.EquipmentMaterial
        });

        return Ok(equipmentList);
    }

    // POST: api/Equipment/importNewEquipment
    [HttpPost("importNewEquipment")]
    public async Task<ActionResult<Equipment>> PostEquipment([FromBody] Equipment equipment)
    {
        if (equipment == null)
        {
            return BadRequest();
        }

        // Check if equipment code already exists
        var existingEquipment = (await _firebaseClient
            .Child("equipment")
            .OnceAsync<Equipment>())
            .FirstOrDefault(e => e.Object.EquipmentCode == equipment.EquipmentCode);

        if (existingEquipment != null)
        {
            return Conflict(new { message = "THIẾT BỊ ĐÃ CÓ SẴN TRONG HỆ THỐNG, KHÔNG THỂ THÊM MỚI" });
        }

        // Add course to Firebase and use auto-generated key
        var result = await _firebaseClient
            .Child("equipment")
            .PostAsync(new
            {
                equipmentBrand = equipment.EquipmentBrand,
                equipmentCategoryId = equipment.EquipmentCategoryId,
                equipmentCode = equipment.EquipmentCode,
                equipmentImportPrice = equipment.EquipmentImportPrice,
                equipmentManufactured = equipment.EquipmentManufactured,
                equipmentMaterial = equipment.EquipmentMaterial,
                equipmentName = equipment.EquipmentName,
                equipmentQuantity = equipment.EquipmentQuantity,
                equipmentSize = equipment.EquipmentSize,
                equipmentWeightStack = equipment.EquipmentWeightStack,
                trainingRoomId = equipment.TrainingRoomId
            });

        // Set the CourseId to Firebase's auto-generated key
        equipment.EquipmentId = result.Key;
        return CreatedAtAction(nameof(GetEquipmentById), new { id = equipment.EquipmentId }, equipment);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Equipment>> GetEquipmentById(string id)
    {
        var equipment = await _firebaseClient
            .Child("equipment")
            .Child(id.ToString())
            .OnceSingleAsync<Equipment>();

        if (equipment == null)
        {
            return NotFound();
        }

        equipment.EquipmentId = id; // Set CourseId based on the requested ID
        return Ok(equipment);
    }

    // GET: api/Equipment/searchByCode?equipmentCode={equipmentCode}
    [HttpGet("searchByCode")]
    public async Task<ActionResult<object>> SearchByEquipmentCode([FromQuery] string equipmentCode)
    {
        if (string.IsNullOrEmpty(equipmentCode))
        {
            return BadRequest(new { message = "Equipment code is required." });
        }

        // Load training rooms and categories to ensure consistent structure
        var trainingRoomsTask = _firebaseClient.Child("TrainingRoom").OnceAsync<TrainingRoom>();
        var equipmentCategoryTask = _firebaseClient.Child("EquipmentCategory").OnceAsync<EquipmentCategory>();

        await Task.WhenAll(trainingRoomsTask, equipmentCategoryTask);

        var trainingRooms = trainingRoomsTask.Result.ToDictionary(tr => tr.Object.TrainingRoomId, tr => tr.Object.TrainingRoomName);
        var equipmentCategories = equipmentCategoryTask.Result.ToDictionary(ec => ec.Object.EquipmentCategoryId, ec => ec.Object.EquipmentCategoryName);

        // Search for equipment by EquipmentCode
        var equipmentResult = (await _firebaseClient
            .Child("equipment")
            .OnceAsync<Equipment>())
            .FirstOrDefault(e => e.Object.EquipmentCode == equipmentCode);

        if (equipmentResult == null)
        {
            return NotFound(new { message = "No equipment found with the specified code." });
        }

        // Set the EquipmentId from Firebase key and map additional properties
        var equipment = equipmentResult.Object;
        equipment.EquipmentId = equipmentResult.Key;

        // Create a response object with the same structure as GetEquipments
        var response = new
        {
            EquipmentId = equipment.EquipmentId,
            equipment.EquipmentName,
            equipment.EquipmentCode,
            equipment.EquipmentImportPrice,
            equipment.EquipmentBrand,
            equipment.EquipmentQuantity,
            equipment.EquipmentCategoryId,
            EquipmentCategoryName = equipmentCategories.GetValueOrDefault(equipment.EquipmentCategoryId, "Unknown"),
            equipment.TrainingRoomId,
            TrainingRoomName = trainingRooms.GetValueOrDefault(equipment.TrainingRoomId, "Unknown"),
            equipment.EquipmentManufactured,
            equipment.EquipmentSize,
            equipment.EquipmentWeightStack,
            equipment.EquipmentMaterial
        };

        return Ok(response);
    }



    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEquipment(string id, [FromBody] Equipment updatedEquipment)
    {
        if (updatedEquipment == null)
        {
            return BadRequest();
        }

        // Check if another equipment has the same code
        var existingEquipmentWithCode = (await _firebaseClient
            .Child("equipment")
            .OnceAsync<Equipment>())
            .FirstOrDefault(e => e.Object.EquipmentCode == updatedEquipment.EquipmentCode && e.Key != id);

        if (existingEquipmentWithCode != null)
        {
            return Conflict(new { message = "Mã thiết bị đã tồn tại trong hệ thống, vui lòng nhập mã khác." });
        }

        // Fetch the existing equipment by ID
        var existingEquipment = await _firebaseClient
            .Child("equipment")
            .Child(id)
            .OnceSingleAsync<Equipment>();

        if (existingEquipment == null)
        {
            return NotFound();
        }

        // Update the equipment data in Firebase
        await _firebaseClient
            .Child("equipment")
            .Child(id)
            .PutAsync(new
            {
                equipmentBrand = updatedEquipment.EquipmentBrand,
                equipmentCategoryId = updatedEquipment.EquipmentCategoryId,
                equipmentCode = updatedEquipment.EquipmentCode,
                equipmentImportPrice = updatedEquipment.EquipmentImportPrice,
                equipmentManufactured = updatedEquipment.EquipmentManufactured,
                equipmentMaterial = updatedEquipment.EquipmentMaterial,
                equipmentName = updatedEquipment.EquipmentName,
                equipmentQuantity = updatedEquipment.EquipmentQuantity,
                equipmentSize = updatedEquipment.EquipmentSize,
                equipmentWeightStack = updatedEquipment.EquipmentWeightStack,
                trainingRoomId = updatedEquipment.TrainingRoomId
            });

        return NoContent();
    }

    [HttpPut("updateWithoutCodeCheck/{id}")]
    public async Task<IActionResult> UpdateEquipmentWithoutCodeCheck(string id, [FromBody] Equipment updatedEquipment)
    {
        if (updatedEquipment == null)
        {
            return BadRequest(new { message = "Dữ liệu thiết bị không hợp lệ." });
        }

        // Fetch the existing equipment by ID
        var existingEquipment = await _firebaseClient
            .Child("equipment")
            .Child(id)
            .OnceSingleAsync<Equipment>();

        if (existingEquipment == null)
        {
            return NotFound(new { message = "Thiết bị không tồn tại." });
        }

        // Update the equipment data in Firebase without checking for duplicate codes
        await _firebaseClient
            .Child("equipment")
            .Child(id)
            .PutAsync(new
            {
                equipmentBrand = updatedEquipment.EquipmentBrand,
                equipmentCategoryId = updatedEquipment.EquipmentCategoryId,
                equipmentCode = updatedEquipment.EquipmentCode,
                equipmentImportPrice = updatedEquipment.EquipmentImportPrice,
                equipmentManufactured = updatedEquipment.EquipmentManufactured,
                equipmentMaterial = updatedEquipment.EquipmentMaterial,
                equipmentName = updatedEquipment.EquipmentName,
                equipmentQuantity = updatedEquipment.EquipmentQuantity,
                equipmentSize = updatedEquipment.EquipmentSize,
                equipmentWeightStack = updatedEquipment.EquipmentWeightStack,
                trainingRoomId = updatedEquipment.TrainingRoomId
            });

        return Ok(new { message = "Thiết bị đã được cập nhật thành công." });
    }


    [HttpPut("UpdateImport/{id}")]
    public async Task<IActionResult> UpdateImport(string id, [FromBody] Equipment updatedEquipment, int importQuantity)
    {
        if (updatedEquipment == null)
        {
            return BadRequest(new { message = "Dữ liệu thiết bị không hợp lệ." });
        }

        // Tìm thiết bị hiện có theo ID
        var existingEquipment = await _firebaseClient
            .Child("equipment")
            .Child(id)
            .OnceSingleAsync<Equipment>();

        if (existingEquipment == null)
        {
            return NotFound(new { message = "Thiết bị không tồn tại." });
        }

        // Chỉ cập nhật số lượng và giá nhập của thiết bị
        await _firebaseClient
            .Child("equipment")
            .Child(id)
            .PutAsync(new
            {
                equipmentBrand = updatedEquipment.EquipmentBrand,
                equipmentCategoryId = updatedEquipment.EquipmentCategoryId,
                equipmentCode = updatedEquipment.EquipmentCode,
                equipmentQuantity = updatedEquipment.EquipmentQuantity + importQuantity,
                equipmentManufactured = updatedEquipment.EquipmentManufactured,
                equipmentMaterial = updatedEquipment.EquipmentMaterial,
                equipmentName = updatedEquipment.EquipmentName,
                equipmentImportPrice = updatedEquipment.EquipmentImportPrice,
                equipmentSize = updatedEquipment.EquipmentSize,
                equipmentWeightStack = updatedEquipment.EquipmentWeightStack,
                trainingRoomId = updatedEquipment.TrainingRoomId
            });

        return Ok(new { message = "Số lượng và giá nhập thiết bị đã được cập nhật thành công." });
    }


    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEquipment(string id)
    {
        // Tìm thiết bị theo ID
        var existingEquipment = await _firebaseClient
            .Child("equipment")
            .Child(id)
            .OnceSingleAsync<Equipment>();

        if (existingEquipment == null)
        {
            return NotFound(new { message = "Thiết bị không tồn tại." });
        }

        // Cập nhật số lượng của thiết bị về 0 thay vì xóa
        await _firebaseClient
            .Child("equipment")
            .Child(id)
            .PutAsync(new
            {
                existingEquipment.EquipmentBrand,
                existingEquipment.EquipmentCategoryId,
                existingEquipment.EquipmentCode,
                equipmentImportPrice = existingEquipment.EquipmentImportPrice,
                existingEquipment.EquipmentManufactured,
                existingEquipment.EquipmentMaterial,
                existingEquipment.EquipmentName,
                equipmentQuantity = 0, // Đặt số lượng về 0
                existingEquipment.EquipmentSize,
                existingEquipment.EquipmentWeightStack,
                existingEquipment.TrainingRoomId
            });

        return Ok(new { message = "Thiết bị đã được cập nhật với số lượng là 0." });
    }


    [HttpDelete("deleteAllEquipment")]
    public async Task<IActionResult> DeleteAllEquipments()
    {
        // Lấy danh sách tất cả equipment
        var equipments = await _firebaseClient
            .Child("equipment")
            .OnceAsync<Equipment>();

        if (equipments == null || !equipments.Any())
        {
            return NotFound(new { message = "Không có thiết bị nào trong hệ thống để xóa." });
        }

        // Duyệt qua từng equipment và xóa nó
        foreach (var equipment in equipments)
        {
            await _firebaseClient
                .Child("equipment")
                .Child(equipment.Key)
                .DeleteAsync();
        }

        return Ok(new { message = "Tất cả thiết bị đã được xóa thành công." });
    }



    [HttpPost("importExcel")]
    public async Task<IActionResult> ImportExcelFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Vui lòng chọn file Excel để tải lên." });
        }

        // Đăng ký provider cho mã hóa để hỗ trợ encoding 1252
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Đọc file Excel
        using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);

        // Sử dụng AsDataSet từ thư viện bổ trợ ExcelDataReader.DataSet
        var result = reader.AsDataSet();

        if (result.Tables.Count == 0)
        {
            return BadRequest(new { message = "Không có dữ liệu trong file Excel." });
        }

        var dataTable = result.Tables[0];
        var equipments = new List<Equipment>();

        // Duyệt qua từng dòng trong file Excel để chuyển dữ liệu thành JSON
        for (int i = 1; i < dataTable.Rows.Count; i++) // Bỏ qua hàng tiêu đề (index 0)
        {
            var row = dataTable.Rows[i];

            var equipment = new Equipment
            {
                EquipmentName = row[0]?.ToString(),
                EquipmentCode = row[1]?.ToString(),
                EquipmentImportPrice = Convert.ToDecimal(row[2]?.ToString() ?? "0"),
                EquipmentBrand = row[3]?.ToString(),
                EquipmentQuantity = Convert.ToInt32(row[4]?.ToString() ?? "0"),
                EquipmentCategoryId = Convert.ToInt32(row[5]?.ToString() ?? "0"),
                TrainingRoomId = Convert.ToInt32(row[6]?.ToString() ?? "0"),
                EquipmentManufactured = row[7]?.ToString(),
                EquipmentSize = row[8]?.ToString(),
                EquipmentWeightStack = Convert.ToDecimal(row[9]?.ToString() ?? "0"),
                EquipmentMaterial = row[10]?.ToString()
            };

            equipments.Add(equipment);
        }

        // Thêm từng thiết bị vào Firebase
        foreach (var equipment in equipments)
        {
            var existingEquipment = (await _firebaseClient
                .Child("equipment")
                .OnceAsync<Equipment>())
                .FirstOrDefault(e => e.Object.EquipmentCode == equipment.EquipmentCode);

            if (existingEquipment != null)
            {
                continue;
            }

            await _firebaseClient
                .Child("equipment")
                .PostAsync(new
                {
                    equipmentBrand = equipment.EquipmentBrand,
                    equipmentCategoryId = equipment.EquipmentCategoryId,
                    equipmentCode = equipment.EquipmentCode,
                    equipmentImportPrice = equipment.EquipmentImportPrice,
                    equipmentManufactured = equipment.EquipmentManufactured,
                    equipmentMaterial = equipment.EquipmentMaterial,
                    equipmentName = equipment.EquipmentName,
                    equipmentQuantity = equipment.EquipmentQuantity,
                    equipmentSize = equipment.EquipmentSize,
                    equipmentWeightStack = equipment.EquipmentWeightStack,
                    trainingRoomId = equipment.TrainingRoomId
                });
        }

        return Ok(new { message = "File Excel đã được import thành công." });
    }

}