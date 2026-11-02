import ServiceDetailEntity from './service-detail.entity';

export default interface ServiceEntity {
  id: number;
  officeId: number;
  name: string;
  description: string;
  quantity: number;
  maxBookingsPerWeek: number;
  bookingPerTime: number;
  minMax: string;
  image: string;
  serviceDetails: ServiceDetailEntity[];
}
