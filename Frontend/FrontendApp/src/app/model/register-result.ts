export interface RegisterResult {
  success: boolean;
  message?: string;
  errors?: { code: string; description: string }[];
}