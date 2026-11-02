export default interface Booking {
  id?: number | null;
  serviceDetailId: number;
  employeeId: number | null;
  startDatetime: Date | null;
  endDatetime: Date | null;
  createdAt?: Date | null;
}
