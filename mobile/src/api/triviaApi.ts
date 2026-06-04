import apiClient from './client';
import { TriviaGameListItem } from '../features/trivia/types';

export async function getPublishedTriviaGames(): Promise<TriviaGameListItem[]> {
  const response = await apiClient.get<TriviaGameListItem[]>('/trivia-games');
  return response.data;
}
