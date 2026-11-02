export interface BookingRow {
  id: number;
  employeeName: string;
  serviceDetailName: string;
  startDateTime: Date | null;
  endDateTime: Date | null;
}
