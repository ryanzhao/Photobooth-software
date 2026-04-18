export interface ApiEnv {
  Bindings: {
    DB?: D1Database;
    ASSETS?: R2Bucket;
    JWT_SECRET?: string;
  };
}
