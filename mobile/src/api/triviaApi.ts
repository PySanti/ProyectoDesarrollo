import apiClient from './client';
import { TriviaGameListItem } from '../features/trivia/types';

export async function getPublishedTriviaGames(modalidad?: string): Promise<TriviaGameListItem[]> {
  const params = modalidad ? { modalidad } : undefined;
  const response = await apiClient.get<TriviaGameListItem[]>('/trivia-games', { params });
  return response.data;
}
