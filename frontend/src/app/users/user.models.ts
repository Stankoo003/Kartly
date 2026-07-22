/**
 * Frontend mirror of the backend user admin DTOs
 * (backend/src/Kartly.Application/Users/UserModels.cs).
 */
import { PagedResult } from '../products/product.models';

export type { PagedResult };

/** The fixed set of roles — mirrors backend Roles.All. */
export const USER_ROLES = ['Admin', 'Customer'] as const;
export type UserRole = (typeof USER_ROLES)[number];

export interface AppUser {
  id: string;
  email: string;
  role: string;
  isActive: boolean;
}

export interface UserQuery {
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ChangeRoleRequest {
  role: string;
}

export interface SetActiveRequest {
  isActive: boolean;
}
