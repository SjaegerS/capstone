-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: idle_game_db
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `ai_feedback_log`
--

DROP TABLE IF EXISTS `ai_feedback_log`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ai_feedback_log` (
  `feedback_id` bigint NOT NULL AUTO_INCREMENT,
  `user_id` bigint NOT NULL,
  `usage_log_id` bigint DEFAULT NULL,
  `feedback_content` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `pattern_summary` text COLLATE utf8mb4_unicode_ci,
  `quest_suggestion` text COLLATE utf8mb4_unicode_ci,
  `condition_result` enum('BEST','GOOD','NORMAL','BAD','WORST') COLLATE utf8mb4_unicode_ci DEFAULT 'NORMAL',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`feedback_id`),
  KEY `fk_ai_feedback_log_user` (`user_id`),
  KEY `fk_ai_feedback_log_usage` (`usage_log_id`),
  CONSTRAINT `fk_ai_feedback_log_usage` FOREIGN KEY (`usage_log_id`) REFERENCES `phone_usage_log` (`usage_log_id`) ON DELETE SET NULL,
  CONSTRAINT `fk_ai_feedback_log_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ai_feedback_log`
--

LOCK TABLES `ai_feedback_log` WRITE;
/*!40000 ALTER TABLE `ai_feedback_log` DISABLE KEYS */;
/*!40000 ALTER TABLE `ai_feedback_log` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-05-05 18:08:33
