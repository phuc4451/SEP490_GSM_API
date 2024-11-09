namespace Alpha_API.Models
{
    public class Equipment
    {
        public string EquipmentId { get; set; }  
        public string EquipmentName { get; set; }
        public string EquipmentCode { get; set; }
        public decimal EquipmentImportPrice { get; set; }
        public string EquipmentBrand { get; set; } 
        public int EquipmentQuantity { get; set; }
        public int EquipmentCategoryId { get; set; }
        public int TrainingRoomId { get; set; }
        public string EquipmentManufactured { get; set; }
        public string EquipmentSize { get; set; }
        public decimal EquipmentWeightStack { get; set; }
        public string EquipmentMaterial { get; set; }
    }

}
