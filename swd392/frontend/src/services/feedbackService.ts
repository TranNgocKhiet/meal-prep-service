import apiClient from '../config/api';
import type { CreateFeedbackDto, FeedbackDto, FeedbackListDto } from '../types/feedback';

const feedbackAPI = {
  // Customer: Create feedback
  createFeedback: async (data: CreateFeedbackDto): Promise<FeedbackDto> => {
    const response = await apiClient.post('/feedbacks', data);
    return response.data.data;
  },

  // Customer: Get their own feedbacks
  getMyFeedbacks: async (): Promise<FeedbackDto[]> => {
    const response = await apiClient.get('/feedbacks/my-feedbacks/list');
    return response.data.data;
  },

  // Get specific feedback by ID
  getFeedbackById: async (feedbackId: string): Promise<FeedbackDto> => {
    const response = await apiClient.get(`/feedbacks/${feedbackId}`);
    return response.data.data;
  },

  // Manager/Admin: Get all feedbacks with pagination
  getAllFeedbacks: async (page: number = 1, pageSize: number = 10): Promise<FeedbackListDto> => {
    const response = await apiClient.get('/feedbacks/all', {
      params: { page, pageSize }
    });
    return response.data.data;
  }
};

export default feedbackAPI;
