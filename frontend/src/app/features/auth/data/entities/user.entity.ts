import UserRole from "./role.entity";

export default interface UserEntity {
    role: UserRole;
    id: string;
}
