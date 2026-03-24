export interface CreateFeedbackDto {
  title: string;
  content: string;
}

export interface FeedbackDto {
  id: string;
  customerId: string;
  customerName: string;
  title: string;
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface FeedbackListDto {
  feedbacks: FeedbackDto[];
  total: number;
  page: number;
  pageSize: number;
}
