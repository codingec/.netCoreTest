export default interface BookingEntity {
  id: number;
  serviceDetailId: number;
  employeeId: number | null;
  startDatetime: Date | null;
  endDatetime: Date | null;
  createdAt: Date | null;
}
