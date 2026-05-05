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
-- Table structure for table `user_quest`
--

DROP TABLE IF EXISTS `user_quest`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `user_quest` (
  `user_quest_id` bigint NOT NULL AUTO_INCREMENT,
  `user_id` bigint NOT NULL,
  `quest_id` bigint NOT NULL,
  `progress_value` int DEFAULT '0',
  `is_accepted` tinyint(1) DEFAULT '0',
  `is_completed` tinyint(1) DEFAULT '0',
  `is_reward_claimed` tinyint(1) DEFAULT '0',
  `assigned_date` date NOT NULL,
  `completed_at` datetime DEFAULT NULL,
  PRIMARY KEY (`user_quest_id`),
  KEY `fk_user_quest_user` (`user_id`),
  KEY `fk_user_quest_quest` (`quest_id`),
  CONSTRAINT `fk_user_quest_quest` FOREIGN KEY (`quest_id`) REFERENCES `quest` (`quest_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_user_quest_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `user_quest`
--

LOCK TABLES `user_quest` WRITE;
/*!40000 ALTER TABLE `user_quest` DISABLE KEYS */;
INSERT INTO `user_quest` VALUES (1,1,1,0,1,0,0,'2026-05-05',NULL),(2,1,2,0,1,0,0,'2026-05-05',NULL),(3,1,4,0,1,0,0,'2026-05-05',NULL);
/*!40000 ALTER TABLE `user_quest` ENABLE KEYS */;
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
