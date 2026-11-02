import ServiceDetailModel from './service-detail.model';

export default interface ServiceModel {
  id: number;
  officeId: number;
  name: string;
  description: string;
  quantity: number;
  maxBookingsPerWeek: number;
  bookingPerTime: number;
  minMax: string;
  imageUrl: string;
  serviceDetails: ServiceDetailModel[];
}
